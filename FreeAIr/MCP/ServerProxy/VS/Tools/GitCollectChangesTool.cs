using FreeAIr.Git;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    /// <summary>The `GitCollectChanges` MCP tool: hands the model the current uncommitted diff, e.g. so it can write a commit message before calling <see cref="GitCommitTool"/>.</summary>
    public sealed class GitCollectChangesTool : VisualStudioMcpServerTool
    {
        /// <summary>Shared singleton instance registered with the MCP tool catalog.</summary>
        public static readonly GitCollectChangesTool Instance = new();

        /// <summary>The MCP tool name exposed to the model.</summary>
        public const string VisualStudioToolName = "GitCollectChanges";

        /// <summary>Registers the tool under <see cref="VisualStudioToolName"/> with no input parameters.</summary>
        public GitCollectChangesTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
                VisualStudioToolName,
                "Returns an uncommitted git changes in plain text format.",
                NoParameters
                )
        {
        }

        /// <summary>Collects the uncommitted diff via <see cref="GitDiffCollector.CollectDiffAsync"/>.</summary>
        public override async Task<McpServerProxyToolCallResult?> CallToolAsync(
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
                return McpServerProxyToolCallResult.CreateFailed($"Collecting changes fail.");
            }

            return McpServerProxyToolCallResult.CreateSuccess(diffs);
        }
    }

}
