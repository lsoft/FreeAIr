using Dto;
using ModelContextProtocol.Client;
using Proxy.Server.Github;

namespace Proxy.Server.External
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
