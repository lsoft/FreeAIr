using FreeAIr.Helper;
using System.Collections.Generic;
using System.Linq;
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

            var payload = await GithubRequestFactory.IsInstalledRequestAsync(
                );

            var reply = await McpServerProxyApplication.ProxyInterface.IsInstalledAsync(payload);

            return reply.IsInstalled;
        }

        public async Task InstallAsync()
        {
            if (!McpServerProxyApplication.Started)
            {
                throw new InvalidOperationException("Proxy application is not started");
            }

            var reply = await McpServerProxyApplication.ProxyInterface.InstallAsync(
                await GithubRequestFactory.InstallRequestAsync(
                    )
                );
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

            var reply = await McpServerProxyApplication.ProxyInterface.GetToolsAsync(
                await GithubRequestFactory.GetToolsRequestAsync(
                    )
                );
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
                var reply = await McpServerProxyApplication.ProxyInterface.CallToolAsync(
                    await GithubRequestFactory.CallToolRequestAsync(
                        toolName,
                        arguments
                        ),
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

                return McpServerProxyToolCallResult.CreateSuccess(
                    reply.Content
                    );
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();

                return McpServerProxyToolCallResult.CreateFailed(
                    excp.Message + Environment.NewLine + excp.StackTrace
                    );
            }

        }
    }
}
