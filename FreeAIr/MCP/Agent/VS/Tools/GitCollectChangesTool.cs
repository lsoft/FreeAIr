using FreeAIr.Git;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.VS.Tools
{
    public sealed class GitCollectChangesTool : VisualStudioAgentTool
    {
        public static readonly GitCollectChangesTool Instance = new();

        public const string VisualStudioToolName = "GitCollectChanges";

        public GitCollectChangesTool(
            ) : base(
                VisualStudioAgent.VisualStudioAgentName,
                VisualStudioToolName,
                "Returns an uncommitted git changes in plain text format.",
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

            var diffs = await GitDiffCombiner.CollectDiffAsync(
                cancellationToken
                );
            if (diffs is null || diffs.Count == 0)
            {
                return new AgentToolCallResult($"Collecting changes fail.");
            }

            return new AgentToolCallResult(diffs);
        }
    }

}
