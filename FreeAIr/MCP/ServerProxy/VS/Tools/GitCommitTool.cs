using FreeAIr.Git;
using FreeAIr.Helper;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    public sealed class GitCommitTool : VisualStudioMcpServerTool
    {
        public static readonly GitCommitTool Instance = new();

        public const string VisualStudioToolName = "GitCommit";

        private const string CommitMessageParameterName = "commit_message";

        public GitCommitTool(
            ) : base(
                VisualStudioMcpServerProxy.VisualStudioProxyName,
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

        public override async Task<McpServerProxyToolCallResult?> CallToolAsync(
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
                    return new McpServerProxyToolCallResult($"Parameter {CommitMessageParameterName} does not found.");
                }
                var commitMessage = commitMessageParameterName as string;

                await GitRunner.CommitAsync(
                    commitMessage,
                    cancellationToken
                    );

                return new McpServerProxyToolCallResult("Successfully committed.");
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();

                return new McpServerProxyToolCallResult("Failed commit.");
            }
        }
    }

}
