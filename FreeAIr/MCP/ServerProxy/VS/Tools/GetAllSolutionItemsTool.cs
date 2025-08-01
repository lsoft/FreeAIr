﻿using FreeAIr.Embedding.Json;
using FreeAIr.Helper;
using FreeAIr.NLOutline.Tree;
using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    public sealed class GetAllSolutionItemsTool : VisualStudioMcpServerTool
    {
        public static readonly GetAllSolutionItemsTool Instance = new();

        public const string VisualStudioToolName = "GetAllSolutionItems";

        public GetAllSolutionItemsTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Returns a JSON-formatted list of items (documents, files) metadata of an open solution. The result of this function includes: item name, item full path, item kind, and optional additional information about item content.",
                "{}"
                )
        {
        }

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

                var jsonEmbeddingFilePath = await EmbeddingOutlineJsonObject.GenerateFilePathAsync();
                var existingOutlineRoot = await OutlineNode.CreateAsync(
                    jsonEmbeddingFilePath,
                    true
                    );

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

                            existingOutlineRoot.ApplyRecursive(
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

                return new McpServerProxyToolCallResult([result]);
            }
            catch (Exception excp)
            {
                throw;
            }
        }

        private sealed class SolutionItemsJson
        {
            public SolutionItemJson[] SolutionItems
            {
                get;
                set;
            }
        }

        private sealed class SolutionItemJson
        {
            public string ItemName
            {
                get;
                set;
            }

            public string ItemType
            {
                get;
                set;
            }

            public string? ItemFullPath
            {
                get;
                set;
            }

            public List<string> ItemNaturalLanguageOutlines
            {
                get;
                set;
            }
        }
    }

}
