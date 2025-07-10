using FreeAIr.Helper;
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

            if (!arguments.TryGetValue(ItemNamePathParameterName, out var itemNamePathObj))
            {
                return new McpServerProxyToolCallResult($"Parameter {ItemNamePathParameterName} does not found.");
            }

            var itemNamePath = itemNamePathObj as string;
             
            var solution = await Community.VisualStudio.Toolkit.VS.Solutions.GetCurrentSolutionAsync();
            if (solution is null)
            {
                return null;
            }

            var items = await solution.ProcessDownRecursivelyForAsync(
                item => !item.IsNonVisibleItem && (StringComparer.InvariantCultureIgnoreCase.Compare(item.Text, itemNamePath) == 0 || StringComparer.InvariantCultureIgnoreCase.Compare(item.FullPath, itemNamePath) == 0),
                false,
                cancellationToken
                );
            var item = items.FirstOrDefault(i => !i.SolutionItem.IsNonVisibleItem);
            if (item is null)
            {
                return new McpServerProxyToolCallResult($"File {itemNamePath} does not found in current solution.");
            }

            var packed = new SolutionItemBodiesJson
            {
                SolutionItemBodies =
                [
                    new SolutionItemBodyJson
                    {
                        ItemName = item.SolutionItem.Name,
                        ItemFullPath = item.SolutionItem.FullPath,
                        ItemType = item.SolutionItem.Type.ToString(),
                        ItemBody = System.IO.File.ReadAllText(item.SolutionItem.FullPath)
                    }
                ]
            };

            var result = JsonSerializer.Serialize(packed);

            return new McpServerProxyToolCallResult([result]);
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
