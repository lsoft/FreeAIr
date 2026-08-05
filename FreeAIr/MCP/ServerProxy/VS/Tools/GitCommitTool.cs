using FreeAIr.Git;
using FreeAIr.Helper;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    /// <summary>The `GitCommit` MCP tool: commits the solution's current changes with a model-supplied message, through <see cref="GitRunner"/>.</summary>
    public sealed class GitCommitTool : VisualStudioMcpServerTool
    {
        /// <summary>
        /// The single shared instance of this tool, registered by <see cref="VisualStudioMcpServerProxy"/>.
        /// </summary>
        public static readonly GitCommitTool Instance = new();

        /// <summary>
        /// The tool name advertised to the chat model for the git commit operation.
        /// </summary>
        public const string VisualStudioToolName = "GitCommit";

        /// <summary>
        /// JSON schema key for the commit message to use.
        /// </summary>
        private const string CommitMessageParameterName = "commit_message";

        /// <summary>
        /// Declares the tool's name and JSON schema describing the required commit_message parameter.
        /// </summary>
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

        /// <summary>Runs `git commit` with the requested message via <see cref="GitRunner.CommitAsync"/>.</summary>
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
                    return McpServerProxyToolCallResult.CreateFailed($"Parameter {CommitMessageParameterName} does not found.");
                }
                var commitMessage = commitMessageParameterName as string;

                await GitRunner.CommitAsync(
                    commitMessage,
                    cancellationToken
                    );

                return McpServerProxyToolCallResult.CreateSuccess("Successfully committed.");
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();

                return McpServerProxyToolCallResult.CreateFailed("Failed to commit.");
            }
        }
    }

}
