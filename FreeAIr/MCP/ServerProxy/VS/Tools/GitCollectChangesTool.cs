using FreeAIr.Git;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    public sealed class GitCollectChangesTool : VisualStudioMcpServerTool
    {
        public static readonly GitCollectChangesTool Instance = new();

        public const string VisualStudioToolName = "GitCollectChanges";

        public GitCollectChangesTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Returns an uncommitted git changes in plain text format.",
                "{}"
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

            var diffs = await GitDiffCollector.CollectDiffAsync(
                cancellationToken
                );
            if (diffs is null || diffs.Count == 0)
            {
                return new McpServerProxyToolCallResult($"Collecting changes fail.");
            }

            return new McpServerProxyToolCallResult(diffs);
        }
    }

}
