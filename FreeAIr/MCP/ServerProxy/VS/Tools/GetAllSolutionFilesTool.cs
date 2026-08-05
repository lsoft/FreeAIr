using FreeAIr.Embedding;
using FreeAIr.Helper;
using FreeAIr.NLOutline.Tree;
using FreeAIr.Options2;
using FreeAIr.Shared.Helper;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    /// <summary>
    /// The `GetAllSolutionFiles` MCP tool: lists every visible file in the open solution together
    /// with the natural-language outline (NLO) summaries already indexed for it, so a model can get
    /// an overview of the codebase without reading every file's full body.
    /// </summary>
    public sealed class GetAllSolutionFilesTool : VisualStudioMcpServerTool
    {
        /// <summary>The single shared instance of this tool, registered with the MCP proxy.</summary>
        public static readonly GetAllSolutionFilesTool Instance = new();

        /// <summary>The MCP tool name the AI chat calls to invoke this tool.</summary>
        public const string VisualStudioToolName = "GetAllSolutionFiles";

        /// <summary>Registers the tool with the proxy under its name and JSON schema (this tool takes no parameters).</summary>
        public GetAllSolutionFilesTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Returns a JSON-formatted list of files (documents, items) metadata of an open solution. The result of this function includes: file name, file full path, file kind, and optional additional information about file's content.",
                NoParameters
                )
        {
        }

        /// <summary>Walks the open solution's visible items and, for each, attaches any file/class-level outline text already computed by the embedding index.</summary>
        public override async Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var solution = await Community.VisualStudio.Toolkit.VS.Solutions.GetCurrentSolutionAsync();
                if (solution is null)
                {
                    return null;
                }

                var items = await solution.ProcessDownRecursivelyForAsync(
                    item => !item.IsNonVisibleItem,
                    false,
                    cancellationToken
                    );

                //without the vectors: this tool reports the outline texts and nothing else, and the
                //vectors are the megabytes of the index. The container keeps the parsed files, so
                //a second call does not reread them either.
                var componentModel = (IComponentModel)await FreeAIrPackage.Instance.GetServiceAsync(typeof(SComponentModel));
                var indexContainer = componentModel.GetService<EmbeddingIndexContainer>();

                var existingOutlineRoot = await indexContainer.GetOutlineTreeAsync(
                    false,
                    cancellationToken: cancellationToken
                    );

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                var converted = items
                    .Select(i =>
                    {
                        var sij =  new SolutionItemJson
                        {
                            ItemName = i.SolutionItem.Name,
                            ItemType = i.SolutionItem.Type.ToString(),
                            ItemFullPath = i.SolutionItem.FullPath,
                            ItemNaturalLanguageOutlines = []
                        };

                        if (!string.IsNullOrEmpty(i.SolutionItem.FullPath))
                        {
                            var sijrp = i.SolutionItem.FullPath.MakeRelativeAgainst(solution.FullPath);

                            existingOutlineRoot?.ApplyRecursive(
                                node =>
                                {
                                    if (sijrp != node.RelativePath)
                                    {
                                        return;
                                    }
                                    if (string.IsNullOrEmpty(node.OutlineText))
                                    {
                                        return;
                                    }
                                    if (node.Kind.NotIn(
                                        OutlineKindEnum.File,
                                        OutlineKindEnum.ClassOrSimilarEntity
                                        )
                                        )
                                    {
                                        return;
                                    }

                                    sij.ItemNaturalLanguageOutlines.Add(
                                        node.OutlineText
                                        );
                                }
                                );
                        }


                        return sij;
                    })
                    .ToArray();
                var packed = new SolutionItemsJson
                {
                    SolutionItems = converted
                };

                var result = JsonSerializer.Serialize(packed);

                return McpServerProxyToolCallResult.CreateSuccess(result);
            }
            catch (Exception excp)
            {
                throw;
            }
        }

        /// <summary>The tool's JSON result: the flat list of solution items and their outlines.</summary>
        private sealed class SolutionItemsJson
        {
            /// <summary>The list of visible solution items, one entry per file, project or folder.</summary>
            public SolutionItemJson[] SolutionItems
            {
                get;
                set;
            }
        }

        /// <summary>One solution item's name, path, kind and any natural-language outline text found for it.</summary>
        private sealed class SolutionItemJson
        {
            /// <summary>The item's display name (file name).</summary>
            public string ItemName
            {
                get;
                set;
            }

            /// <summary>The kind of solution item (e.g. physical file, project, folder).</summary>
            public string ItemType
            {
                get;
                set;
            }

            /// <summary>Full path of the item on disk, if it has one.</summary>
            public string? ItemFullPath
            {
                get;
                set;
            }

            /// <summary>NLO summaries (file and top-level type outlines) already indexed for this item, if any.</summary>
            public List<string> ItemNaturalLanguageOutlines
            {
                get;
                set;
            }
        }
    }

}
