using FreeAIr.Helper;
using MessagePack.Resolvers;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    public sealed class GetSolutionItemBodyTool : VisualStudioMcpServerTool
    {
        private const string ItemNamePathParameterName = "item_name_or_full_path";

        public static readonly GetSolutionItemBodyTool Instance = new();

        public const string VisualStudioToolName = "GetSolutionItemBody";

        public GetSolutionItemBodyTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Returns a JSON-formatted information about solution item (document, file). The result of this function includes: item name, item full path, item kind and its content (body, text) for each found item.",
                $$$"""
                {
                    "type": "object",
                    "properties": {
                        "{{{ItemNamePathParameterName}}}": {
                        "type": "string",
                        "description": "Full path or name of solution item"
                        }
                    },
                    "required": ["{{{ItemNamePathParameterName}}}"]
                }
                """
                )
        {
        }

        public override async Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!arguments.TryGetValue(ItemNamePathParameterName, out var nameOrPathOfItemObj))
            {
                return McpServerProxyToolCallResult.CreateFailed($"Parameter {ItemNamePathParameterName} does not found.");
            }

            var nameOrPathOfItem = nameOrPathOfItemObj as string;

            var item = await SolutionHelper.FindItemByNameOrFilePathAsync(
                nameOrPathOfItem,
                cancellationToken
                );
            if (item is null)
            {
                return McpServerProxyToolCallResult.CreateFailed($"File {nameOrPathOfItem} does not found in current solution.");
            }

            var itemBody = await SolutionHelper.GetActualItemBodyAsync(item.SolutionItem.FullPath);

            var packed = new SolutionItemBodiesJson
            {
                SolutionItemBodies =
                [
                    new SolutionItemBodyJson
                {
                    ItemName = item.SolutionItem.Name,
                    ItemFullPath = item.SolutionItem.FullPath,
                    ItemType = item.SolutionItem.Type.ToString(),
                    ItemBody = itemBody
                }
                ]
            };

            var result = JsonSerializer.Serialize(packed);

            return McpServerProxyToolCallResult.CreateSuccess(result);
        }


        private sealed class SolutionItemBodiesJson
        {
            public SolutionItemBodyJson[] SolutionItemBodies
            {
                get;
                set;
            }
        }

        private sealed class SolutionItemBodyJson
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

            public string ItemBody
            {
                get;
                set;
            }
        }
    }

}
