using Dto;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.External
{
    public sealed class ExternalAgent : IAgent
    {
        private readonly McpServer _server;

        public string Name
        {
            get;
        }

        public ExternalAgent(
            string name,
            McpServer server
            )
        {
            if (server is null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            _server = server;
        }

        public async Task<bool> IsInstalledAsync()
        {
            if (!AgentApplication.Started)
            {
                return false;
            }

            return true;
        }

        public Task InstallAsync()
        {
            if (!AgentApplication.Started)
            {
                throw new InvalidOperationException("Agent is not started");
            }

            return Task.CompletedTask;
        }

        public async Task<AgentTools> GetToolsAsync()
        {
            if (!AgentApplication.Started)
            {
                throw new InvalidOperationException("Agent is not started");
            }

            var response = await AgentApplication.HttpClient.PostAsJsonAsync<GetToolsRequest>(
                "/get_tools",
                new GetToolsRequest(
                    Name,
                    GetAgentSpecificArguments()
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
                    new CallToolRequest(
                        Name,
                        toolName,
                        arguments,
                        GetAgentSpecificArguments()
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

        private Dictionary<string, string> GetAgentSpecificArguments()
        {
            return new Dictionary<string, string>
            {
                ["MCPServerFolderPath"] = FreeAIrPackage.WorkingFolder,
                ["MCPServerFileName"] = _server.Command,
                ["MCPServerArguments"] = _server.GetArgStringRepresentation(),
                ["MCPServerEnvironment"] = _server.GetEnvStringRepresentation()
            };
        }

    }
}
