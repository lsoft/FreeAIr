using FreeAIr.Options2.Agent;
using FreeAIr.Helper;
using FreeAIr.NLOutline.Tree.Builder.File;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FreeAIr.Options2.Support;

namespace FreeAIr.NLOutline.Tree.Builder
{
    /// <summary>
    /// Builds the natural-language-outline tree for the current solution: walks every project
    /// and text file, and delegates per-file outline extraction/comment generation to
    /// <see cref="FileOutlineTreeProcessor"/>, reusing unchanged file nodes from a previous run
    /// where possible.
    /// </summary>
    public static class TreeBuilder
    {
        /// <summary>
        /// Walks the current solution's projects and text files, building an
        /// <see cref="OutlineNode"/> tree rooted at the solution and populated per project by
        /// <see cref="FileOutlineTreeProcessor.CreateFileTreesAsync"/>; returns null if there is
        /// no open solution.
        /// </summary>
        public static async Task<OutlineNode?> BuildAsync(
            TreeBuilderParameters parameters,
            CancellationToken cancellationToken,
            Action<int, int>? onProgress = null
            )
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution is null)
            {
                return null;
            }

            var rootPath = solution.FullPath;

            var root = new OutlineNode(
                OutlineKindEnum.Solution,
                string.Empty,
                solution.Name,
                string.Empty,
                null,
                []
                );

            var projects = await solution
                .ProcessDownRecursivelyForAsync(
                    i => i.Type == SolutionItemType.Project,
                    false,
                    cancellationToken
                    );

            //gathered up front so the total file count is known before any file is processed,
            //which is what lets the progress callback report "processed/total" instead of a
            //running count with no denominator
            var projectFileLists = new List<(SolutionItem Project, List<SolutionItem> Files)>();
            foreach (var foundProject in projects
                .OrderBy(i => i.SolutionItem.FullPath.MakeRelativeAgainst(solution.FullPath))
                )
            {
                var project = foundProject.SolutionItem;

                var files = await project.ProcessDownRecursivelyForAsync(
                    item =>
                        !item.IsNonVisibleItem
                        && item.Type == SolutionItemType.PhysicalFile
                        && !string.IsNullOrEmpty(item.FullPath)
                        && item.FullPath.GetFileType() == FileTypeEnum.Text
                        //&& (allowedPaths is null || allowedPaths.Contains(item.FullPath))
                        ,
                    false,
                    cancellationToken
                    );

                projectFileLists.Add(
                    (project,
                    files
                        .OrderBy(i => i.SolutionItem.FullPath.MakeRelativeAgainst(solution.FullPath))
                        .Select(i => i.SolutionItem)
                        .ToList())
                    );
            }

            var totalFileCount = projectFileLists.Sum(pf => pf.Files.Count);
            var processedFileCount = 0;

            var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
            var treeProcessor = componentModel.GetService<FileOutlineTreeProcessor>();

            foreach (var (project, files) in projectFileLists)
            {
                var projectRoot = root.AddChild(
                    OutlineKindEnum.Project,
                    project.FullPath.MakeRelativeAgainst(rootPath),
                    project.Name,
                    string.Empty
                    );

                await treeProcessor.CreateFileTreesAsync(
                    parameters,
                    rootPath,
                    projectRoot,
                    files,
                    () =>
                    {
                        processedFileCount++;
                        onProgress?.Invoke(processedFileCount, totalFileCount);
                    }
                    );
            }

            return root;
        }
    }

    /// <summary>
    /// Inputs for one <see cref="TreeBuilder.BuildAsync"/> run: which support action and agent
    /// generate the NLO comments, which files actually changed, and the previous outline tree to
    /// reuse nodes from for files that did not.
    /// </summary>
    public sealed class TreeBuilderParameters
    {
        /// <summary>
        /// The support action whose prompt produces the NLO comments for changed files.
        /// </summary>
        public SupportActionJson Action
        {
            get;
        }

        /// <summary>
        /// The agent used to run <see cref="Action"/>, unless <see cref="ForceUseNLOAgent"/> overrides it.
        /// </summary>
        public AgentJson Agent
        {
            get;
        }

        /// <summary>
        /// Whether to force use of the dedicated NLO agent instead of <see cref="Agent"/>.
        /// </summary>
        public bool ForceUseNLOAgent
        {
            get;
        }

        /// <summary>
        /// Relative paths of files known to have changed since the previous outline build; only
        /// these are reprocessed, everything else is reused from <see cref="OldOutlineRoot"/>.
        /// </summary>
        public HashSet<string> CheckedPaths
        {
            get;
        }

        /// <summary>
        /// The outline tree from the previous build, used to look up and reuse unchanged file nodes.
        /// </summary>
        public OutlineNode OldOutlineRoot
        {
            get;
        }

        /// <summary>
        /// Creates the parameters for a tree build with the given action, agent, changed-file set
        /// and previous outline tree.
        /// </summary>
        public TreeBuilderParameters(
            SupportActionJson action,
            AgentJson agent,
            bool forceUseNLOAgent,
            HashSet<string>? checkedPaths,
            OutlineNode? oldOutlineRoot
            )
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            Action = action;
            Agent = agent;
            ForceUseNLOAgent = forceUseNLOAgent;
            CheckedPaths = checkedPaths;
            OldOutlineRoot = oldOutlineRoot;
        }

        /// <summary>
        /// Looks up the previous build's outline node for the given file, but only if the file
        /// is not in <see cref="CheckedPaths"/> (i.e. it did not change and its old node can be reused).
        /// </summary>
        public bool TryGetFileOutlineNode(
            string relativePath,
            out OutlineNode? oldOutlineNode
            )
        {
            if (CheckedPaths.Contains(relativePath))
            {
                //this node has changes and need to be reprocessed
                oldOutlineNode = null;
                return false;
            }

            OutlineNode? result = null;
            _ = OldOutlineRoot.ApplyRecursive(
                 node =>
                 {
                     if (node.Kind == OutlineKindEnum.File && node.RelativePath == relativePath)
                     {
                         result = node;
                         return false; //break
                     }

                     //continue scanning
                     return true;
                 }
                 );

            oldOutlineNode = result;
            return result is not null;
        }
    }
}
