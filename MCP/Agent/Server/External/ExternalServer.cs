using Agent.Server.Github;
using Dto;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Agent.Server.External
{
    public class ExternalServer : BaseServer2<GithubServer>
    {
        private readonly McpServer _server;

        public string Name
        {
            get;
        }

        public ExternalServer(
            string name,
            McpServer server
            )
        {
            ArgumentException.ThrowIfNullOrEmpty(name);
            ArgumentNullException.ThrowIfNull(server);

            Name = name;
            _server = server;
        }

        protected override async Task<IMcpClient?> CreateMcpClientAsync(
            IParameterProvider parameterProvider
            )
        {
            var mcpServerFolderPath = Environment.CurrentDirectory;
            var mcpServerFileName = _server.Command;

            var arguments = _server.Args;
            var env = _server.Env;

            var mcpClientTransport = new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Name = this.Name,
                    WorkingDirectory = mcpServerFolderPath,
                    Command = mcpServerFileName,
                    ShutdownTimeout = TimeSpan.FromMinutes(1),
                    Arguments = arguments,
                    EnvironmentVariables = env
                }
                );
            return await McpClientFactory.CreateAsync(mcpClientTransport);
        }
    }
}
