using FreeAIr.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    public sealed class ReplaceDocumentBodyTool : VisualStudioMcpServerTool
    {
        public static readonly ReplaceDocumentBodyTool Instance = new();

        public const string VisualStudioToolName = "ReplaceDocumentBody";

        private const string ItemNamePathParameterName = "item_name_or_full_path";
        private const string NewItemBodyParameterName = "new_item_body";

        public ReplaceDocumentBodyTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Replaces the item (document, file) body (content, text) with a new one. Use this function if you need to make a changes in the item (document, file).",
                $$$"""
                {
                    "type": "object",
                    "properties": {
                        "{{{ItemNamePathParameterName}}}": {
                        "type": "string",
                        "description": "Full path or name of the solution item (document, file) in which we change the body (text, content)"
                        },
                        "{{{NewItemBodyParameterName}}}": {
                        "type": "string",
                        "description": "A new body (text, content) of solution item."
                        }
                    },
                    "required": ["{{{ItemNamePathParameterName}}}", "{{{NewItemBodyParameterName}}}"]
                }
                """
                )
        {
        }

        public override async Task<McpServerProxyToolCallResult> CallToolAsync(
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
            if (!System.IO.File.Exists(itemNamePath))
            {
                return new McpServerProxyToolCallResult($"File {itemNamePath} does not found at the disk.");
            }

            if (!arguments.TryGetValue(NewItemBodyParameterName, out var newItemBodyObj))
            {
                return new McpServerProxyToolCallResult($"Parameter {NewItemBodyParameterName} does not found.");
            }
            var newItemBody = newItemBodyObj as string;


            var solution = await Community.VisualStudio.Toolkit.VS.Solutions.GetCurrentSolutionAsync();
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

            var lineEndings = LineEndingHelper.Actual.GetDocumentLineEnding(item.SolutionItem.FullPath);

            var lines = newItemBody.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
                );

            var newBody = string.Join(lineEndings, lines);
            System.IO.File.WriteAllText(itemNamePath, newBody);

            var result = JsonSerializer.Serialize(
                new UpdateBodyResultJson
                {
                    ResultMessage = "The document successfully updated"
                }
                );

            return new McpServerProxyToolCallResult([result]);
        }

        private sealed class UpdateBodyResultJson
        {
            public string ResultMessage
            {
                get;
                set;
            }
        }

    }

}
