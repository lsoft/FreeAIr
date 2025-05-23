using FreeAIr.Helper;
using FreeAIr.MCP.Agent.Github.BLO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.VS.Tools
{
    public sealed class GetAllSolutionItemsTool : VisualStudioAgentTool
    {
        public static readonly GetAllSolutionItemsTool Instance = new();

        public const string VisualStudioToolName = "GetAllSolutionItems";

        public GetAllSolutionItemsTool(
            ) : base(
                VisualStudioAgent.VisualStudioAgentName,
                VisualStudioToolName,
                "Returns a JSON-formatted list of items (documents, files) metadata of an open solution. The result of its function includes: item name, item full path, item kind for each found item.",
                "{}"
                )
        {
        }

        public override async Task<AgentToolCallResult> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var solution = await Community.VisualStudio.Toolkit.VS.Solutions.GetCurrentSolutionAsync();
            var items = await solution.ProcessDownRecursivelyForAsync(
                item => !item.IsNonVisibleItem,
                false
                );

            var converted = items
                .Select(i => new SolutionItemJson
                {
                    ItemName = i.SolutionItem.Name,
                    ItemType = i.SolutionItem.Type.ToString(),
                    ItemFullPath = i.SolutionItem.FullPath,
                })
                .ToArray();
            var packed = new SolutionItemsJson
            {
                SolutionItems = converted
            };

            var result = JsonSerializer.Serialize(packed);

            return new AgentToolCallResult([result]);
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
        }
    }

}
