using FreeAIr.Git;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.VS.Tools
{
    public sealed class GitCommitTool : VisualStudioAgentTool
    {
        public static readonly GitCommitTool Instance = new();

        public const string VisualStudioToolName = "GitCommit";

        private const string CommitMessageParameterName = "commit_message";

        public GitCommitTool(
            ) : base(
                VisualStudioAgent.VisualStudioAgentName,
                VisualStudioToolName,
                "Commits solution changes into git.",
                $$$"""
                {
                    "type": "object",
                    "properties": {
                        "{{{CommitMessageParameterName}}}": {
                            "type": "string",
                            "description": "A commit message"
                            }
                        },
                    "required": ["{{{CommitMessageParameterName}}}"]
                }
                """)
        {
        }

        public override async Task<AgentToolCallResult> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (!arguments.TryGetValue(CommitMessageParameterName, out var commitMessageParameterName))
                {
                    return new AgentToolCallResult($"Parameter {CommitMessageParameterName} does not found.");
                }
                var commitMessage = commitMessageParameterName as string;

                await GitRunner.CommitAsync(
                    commitMessage,
                    cancellationToken
                    );

                return new AgentToolCallResult("Successfully committed.");
            }
            catch (Exception excp)
            {
                //todo log

                return new AgentToolCallResult("Failed commit.");
            }
        }
    }

}
