using Dto;
using FreeAIr.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.External
{
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

        public async Task<bool> IsInstalledAsync()
        {
            if (!McpServerProxyApplication.Started)
            {
                return false;
            }

            return true;
        }

        public Task InstallAsync()
        {
            if (!McpServerProxyApplication.Started)
            {
                throw new InvalidOperationException("Proxy application is not started");
            }

            return Task.CompletedTask;
        }

        public async Task<McpServerTools> GetToolsAsync()
        {
            if (!McpServerProxyApplication.Started)
            {
                throw new InvalidOperationException("Proxy application is not started");
            }

            var response = await McpServerProxyApplication.HttpClient.PostAsJsonAsync<GetToolsRequest>(
                "/get_tools",
                new GetToolsRequest(
                    Name,
                    GetMcpServerSpecificArguments()
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
                    new CallToolRequest(
                        Name,
                        toolName,
                        arguments,
                        GetMcpServerSpecificArguments()
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
                excp.ActivityLogException();

                return new McpServerProxyToolCallResult([excp.Message + Environment.NewLine + excp.StackTrace]);
            }

        }

        private Dictionary<string, string> GetMcpServerSpecificArguments()
        {
            return new Dictionary<string, string>
            {
                ["MCPServerFolderPath"] = FreeAIrPackage.WorkingFolder
            };
        }

    }
}
