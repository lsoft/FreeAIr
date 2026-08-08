using FreeAIr.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.Github
{
    /// <summary>
    /// The VSIX side's handle to the built-in `github.com` server: every call is forwarded across
    /// the pipe with its request assembled by <see cref="GithubRequestFactory"/>, which supplies the
    /// server folder path and the user's PAT that <see cref="Proxy.Server.Github.GithubServer"/>
    /// needs on the other side.
    /// </summary>
    public sealed class GithubMcpServerProxy : IMcpServerProxy
    {
        /// <summary>The name under which the built-in GitHub server is registered and shown to the user.</summary>
        public const string PublicMCPServerName = "github.com";

        /// <summary>The single shared proxy instance for the `github.com` server.</summary>
        public static readonly GithubMcpServerProxy Instance = new();

        /// <summary>The server name reported to callers, matching <see cref="PublicMCPServerName"/>.</summary>
        public string Name => "github.com";

        /// <summary>Prevents external construction; use <see cref="Instance"/> instead.</summary>
        private GithubMcpServerProxy(
            )
        {
        }

        /// <summary>Asks the proxy whether the `github-mcp-server` executable is already unpacked.</summary>
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

        /// <summary>Has the proxy download and unpack the `github-mcp-server` executable.</summary>
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

        /// <summary>Asks the proxy for the GitHub server's tool list.</summary>
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

        /// <summary>Forwards a tool call to the GitHub server, converting a transport failure or a reported tool error into a failed <see cref="McpServerProxyToolCallResult"/> instead of throwing.</summary>
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
