using FreeAIr.Helper;
using FreeAIr.MCP.Agent.Github.BLO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.VS.Tools
{
    public sealed class GetSolutionItemBodyTool : VisualStudioAgentTool
    {
        private const string ParameterName = "item_name_or_full_path";

        public static readonly GetSolutionItemBodyTool Instance = new();

        public const string VisualStudioToolName = "GetSolutionItemBody";

        public GetSolutionItemBodyTool(
            ) : base(
                VisualStudioAgent.VisualStudioAgentName,
                VisualStudioToolName,
                "Returns a JSON-formatted information about solution item (document, file). The result of this function includes: item name, item full path, item kind and its content (body, text) for each found item.",
                $$$"""
                {
                    "type": "object",
                    "properties": {
                        "{{{ParameterName}}}": {
                        "type": "string",
                        "description": "Full path or name of solution item"
                        }
                    },
                    "required": ["{{{ParameterName}}}"]
                }
                """
                )
        {
        }

        public override async Task<AgentToolCallResult> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            if (!arguments.TryGetValue(ParameterName, out var itemNameObj))
            {
                return new AgentToolCallResult($"Parameter {ParameterName} does not found.");
            }

            var itemName = itemNameObj as string;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
             
            var solution = await Community.VisualStudio.Toolkit.VS.Solutions.GetCurrentSolutionAsync();
            var items = await solution.ProcessDownRecursivelyForAsync(
                item => !item.IsNonVisibleItem && (StringComparer.InvariantCultureIgnoreCase.Compare(item.Text, itemName) == 0 || StringComparer.InvariantCultureIgnoreCase.Compare(item.FullPath, itemName) == 0),
                false
                );
            var item = items.FirstOrDefault(i => !i.SolutionItem.IsNonVisibleItem);
            if (item is null)
            {
                return new AgentToolCallResult($"File {itemName} does not found in current solution.");
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

            return new AgentToolCallResult([result]);
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
