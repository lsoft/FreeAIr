using Dto;
using FreeAIr.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.External
{
    /// <summary>
    /// The VSIX side's handle to a user-configured external MCP server: every call is forwarded
    /// across the pipe to <see cref="McpServerProxyApplication.ProxyInterface"/> by
    /// <see cref="Name"/>, which the proxy resolves through its own <c>Servers</c> registry.
    /// </summary>
    public sealed class ExternalMcpServerProxy : IMcpServerProxy
    {
        private readonly McpServer _server;

        public string Name
        {
            get;
        }

        public ExternalMcpServerProxy(
            string name
            )
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
        }

        /// <summary>An external server needs no separate install step - this only checks that the proxy process itself is running.</summary>
        public async Task<bool> IsInstalledAsync()
        {
            if (!McpServerProxyApplication.Started)
            {
                return false;
            }

            return true;
        }

        /// <summary>A no-op beyond requiring the proxy to be running, since an external server has nothing of its own to install.</summary>
        public Task InstallAsync()
        {
            if (!McpServerProxyApplication.Started)
            {
                throw new InvalidOperationException("Proxy application is not started");
            }

            return Task.CompletedTask;
        }

        /// <summary>Asks the proxy for this server's tool list across the pipe.</summary>
        public async Task<McpServerTools> GetToolsAsync()
        {
            if (!McpServerProxyApplication.Started)
            {
                throw new InvalidOperationException("Proxy application is not started");
            }

            var reply = await McpServerProxyApplication.ProxyInterface.GetToolsAsync(
                new GetToolsRequest(
                    Name,
                    GetMcpServerSpecificArguments()
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

        /// <summary>Forwards a tool call across the pipe, converting a transport failure or a reported tool error into a failed <see cref="McpServerProxyToolCallResult"/> instead of throwing.</summary>
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
                    new CallToolRequest(
                        Name,
                        toolName,
                        arguments,
                        GetMcpServerSpecificArguments()
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

        /// <summary>The parameters every call to this server needs regardless of tool - currently just the proxy's working folder.</summary>
        private Dictionary<string, string> GetMcpServerSpecificArguments()
        {
            return new Dictionary<string, string>
            {
                ["MCPServerFolderPath"] = FreeAIrPackage.WorkingFolder
            };
        }

    }
}
