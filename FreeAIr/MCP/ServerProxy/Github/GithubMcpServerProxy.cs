using Dto;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.Github
{
    public sealed class GithubMcpServerProxy : IMcpServerProxy
    {
        public const string PublicMCPServerName = "github.com";

        public static readonly GithubMcpServerProxy Instance = new();

        public string Name => "github.com";

        private GithubMcpServerProxy(
            )
        {
        }

        public async Task<bool> IsInstalledAsync()
        {
            if (!McpServerProxyApplication.Started)
            {
                return false;
            }

            var response = await McpServerProxyApplication.HttpClient.PostAsJsonAsync<IsInstalledRequest>(
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
            if (!McpServerProxyApplication.Started)
            {
                throw new InvalidOperationException("Proxy application is not started");
            }

            var response = await McpServerProxyApplication.HttpClient.PostAsJsonAsync<InstallRequest>(
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

        public async Task<McpServerTools> GetToolsAsync()
        {
            if (!McpServerProxyApplication.Started)
            {
                throw new InvalidOperationException("Proxy application is not started");
            }

            var response = await McpServerProxyApplication.HttpClient.PostAsJsonAsync<GetToolsRequest>(
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

            return new McpServerTools(
                reply.Tools.Select(t => new McpServerTool(Name, t.Name, t.Description, t.Parameters)).ToList()
                );
        }

        public async Task<McpServerProxyToolCallResult> CallToolAsync(
            string toolName,
            Dictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            if (!McpServerProxyApplication.Started)
            {
                throw new InvalidOperationException("Proxy application is not started");
            }

            try
            {
                var response = await McpServerProxyApplication.HttpClient.PostAsJsonAsync<CallToolRequest>(
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

                return new McpServerProxyToolCallResult(
                    reply.Content
                    );
            }
            catch (Exception excp)
            {
                //todo log
                return new McpServerProxyToolCallResult([excp.Message + Environment.NewLine + excp.StackTrace]);
            }

        }
    }
}
