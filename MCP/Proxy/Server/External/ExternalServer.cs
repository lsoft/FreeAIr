using Dto;
using ModelContextProtocol.Client;
using Proxy.Server.Github;

namespace Proxy.Server.External
{
    /// <summary>
    /// A user-configured MCP server imported from an external tool's config (see
    /// <see cref="Dto.McpServers"/>): builds a stdio or SSE/HTTP <see cref="IMcpClient"/> depending
    /// on <see cref="Dto.McpServerType"/>, then behaves like any other <see cref="BaseServer2{T}"/>.
    /// </summary>
    public class ExternalServer : BaseServer2<GithubServer>
    {
        private readonly McpServer _server;

        /// <summary>The server's display name, as configured on the VS side; also used as the transport's client name.</summary>
        public string Name
        {
            get;
        }

        /// <summary>Wraps an externally configured server's stored connection info under <paramref name="name"/>.</summary>
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

        /// <summary>Builds the stdio or HTTP transport described by this server's stored configuration and connects an <see cref="IMcpClient"/> over it.</summary>
        protected override async Task<IMcpClient?> CreateMcpClientAsync(
            IParameterProvider parameterProvider
            )
        {
            IClientTransport mcpClientTransport;
            switch (_server.Type)
            {
                case McpServerType.Stdio:
                    {
                        var parameters = StdioMcpServerParameters.DeserializeStdio(
                            _server
                            );

                        var mcpServerFolderPath = Environment.CurrentDirectory;
                        var mcpServerFileName = parameters.Command;
                        var arguments = parameters.Args;
                        var env = parameters.Env;

                        mcpClientTransport = new StdioClientTransport(
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
                    }
                    break;
                case McpServerType.Http:
                    {
                        var parameters = HttpMcpServerParameters.DeserializeStdio(
                            _server
                            );

                        mcpClientTransport = new SseClientTransport(
                            new SseClientTransportOptions
                            {
                                TransportMode = HttpTransportMode.AutoDetect,
                                Name = this.Name,
                                Endpoint = new Uri(parameters.Endpoint)
                            }
                            );
                    }
                    break;
                default:
                    throw new InvalidOperationException($"Unknown type: {_server.Type}");
            }

            return await McpClientFactory.CreateAsync(mcpClientTransport);
        }
    }
}
