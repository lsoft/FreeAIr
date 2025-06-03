using Dto;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.Github
{
    public sealed class GithubAgent : IAgent
    {
        public const string PublicMCPServerName = "github.com";

        public static readonly GithubAgent Instance = new();

        public string Name => "github.com";

        private GithubAgent(
            )
        {
        }

        public async Task<bool> IsInstalledAsync()
        {
            if (!AgentApplication.Started)
            {
                return false;
            }

            var response = await AgentApplication.HttpClient.PostAsJsonAsync<IsInstalledRequest>(
                "/is_installed",
                GithubRequestFactory.IsInstalledRequest(
                    )
                );
            response.EnsureSuccessStatusCode();

            var reply = await response.Content.ReadFromJsonAsync<IsInstalledReply>();
            return reply.IsInstalled;
        }

        public async Task InstallAsync()
        {
            if (!AgentApplication.Started)
            {
                throw new InvalidOperationException("Agent is not started");
            }

            var response = await AgentApplication.HttpClient.PostAsJsonAsync<InstallRequest>(
                "/install",
                GithubRequestFactory.InstallRequest(
                    )
                );
            response.EnsureSuccessStatusCode();

            var reply = await response.Content.ReadFromJsonAsync<InstallReply>();
            if (!string.IsNullOrEmpty(reply.ErrorMessage))
            {
                throw new InvalidOperationException(reply.ErrorMessage);
            }
        }

        public async Task<AgentTools> GetToolsAsync()
        {
            if (!AgentApplication.Started)
            {
                throw new InvalidOperationException("Agent is not started");
            }

            var response = await AgentApplication.HttpClient.PostAsJsonAsync<GetToolsRequest>(
                "/get_tools",
                GithubRequestFactory.GetToolsRequest(
                    )
                );
            response.EnsureSuccessStatusCode();

            var reply = await response.Content.ReadFromJsonAsync<GetToolsReply>();

            if (!string.IsNullOrEmpty(reply.ErrorMessage))
            {
                throw new InvalidOperationException(reply.ErrorMessage);
            }

            return new AgentTools(
                reply.Tools.Select(t => new AgentTool(Name, t.Name, t.Description, t.Parameters)).ToList()
                );
        }

        public async Task<AgentToolCallResult> CallToolAsync(
            string toolName,
            Dictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            if (!AgentApplication.Started)
            {
                throw new InvalidOperationException("Agent is not started");
            }

            try
            {
                var response = await AgentApplication.HttpClient.PostAsJsonAsync<CallToolRequest>(
                    "/call_tool",
                    GithubRequestFactory.CallToolRequest(
                        toolName,
                        arguments
                        ),
                    cancellationToken
                    );
                response.EnsureSuccessStatusCode();

                var reply = await response.Content.ReadFromJsonAsync<CallToolReply>(
                    cancellationToken
                    );
                if (!string.IsNullOrEmpty(reply.ErrorMessage))
                {
                    throw new InvalidOperationException(reply.ErrorMessage);
                }
                if (reply.IsError)
                {
                    throw new InvalidOperationException("Error during tool call");
                }

                return new AgentToolCallResult(
                    reply.Content
                    );
            }
            catch (Exception excp)
            {
                //todo log
                return new AgentToolCallResult([excp.Message + Environment.NewLine + excp.StackTrace]);
            }

        }
    }
}
