using FreeAIr.Options2.Agent;
using FreeAIr.BLogic;
using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.Helper;
using FreeAIr.NLOutline.Tree.Builder.File;
using Microsoft.VisualStudio.ComponentModelHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.NLOutline.Tree.Builder
{
    public static class TreeBuilder
    {
        public static async Task<OutlineNode> BuildAsync(
            TreeBuilderParameters parameters,
            CancellationToken cancellationToken
            )
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var solution = await VS.Solutions.GetCurrentSolutionAsync();
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

            foreach (var foundProject in projects
                .OrderBy(i => i.SolutionItem.FullPath.MakeRelativeAgainst(solution.FullPath))
                )
            {
                var project = foundProject.SolutionItem;

                //if (allowedPaths is not null && !allowedPaths.Contains(project.FullPath))
                //{
                //    continue;
                //}

                var projectRoot = root.AddChild(
                    OutlineKindEnum.Project,
                    project.FullPath.MakeRelativeAgainst(rootPath),
                    project.Name,
                    string.Empty
                    );

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

                var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
                var treeProcessor = componentModel.GetService<FileOutlineTreeProcessor>();

                await treeProcessor.CreateFileTreesAsync(
                    parameters,
                    rootPath,
                    projectRoot,
                    files
                        .OrderBy(i => i.SolutionItem.FullPath.MakeRelativeAgainst(solution.FullPath))
                        .Select(i => i.SolutionItem)
                        .ToList()
                    );
            }

            return root;
        }
    }

    public sealed class TreeBuilderParameters
    {
        public AgentJson Agent
        {
            get;
        }
        
        public bool ForceUseNLOAgent
        {
            get;
        }

        public HashSet<string> CheckedPaths
        {
            get;
        }

        public OutlineNode OldOutlineRoot
        {
            get;
        }

        public TreeBuilderParameters(
            AgentJson agent,
            bool forceUseNLOAgent,
            HashSet<string>? checkedPaths,
            OutlineNode? oldOutlineRoot
            )
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            Agent = agent;
            ForceUseNLOAgent = forceUseNLOAgent;
            CheckedPaths = checkedPaths;
            OldOutlineRoot = oldOutlineRoot;
        }

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
