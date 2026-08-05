#nullable disable
#pragma warning disable IDE1006
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dto
{
    /// <summary>
    /// The `mcpServers` object as it appears in an external tool's own config file (Claude Desktop,
    /// VS Code, etc.) - the format FreeAIr reads when importing an externally configured server and
    /// writes when exporting one of its own.
    /// </summary>
    public class McpServers : ICloneable
    {
        /// <summary>The configured servers keyed by their display name, as found under the `mcpServers` key of the config file.</summary>
        [JsonPropertyName("mcpServers")]
        public Dictionary<string, McpServer> Servers
        {
            get; set;
        }

        /// <summary>Creates an empty server list.</summary>
        public McpServers()
        {
            Servers = new();
        }

        public object Clone()
        {
            var servers = new Dictionary<string, McpServer>();

            foreach (var pair in Servers)
            {
                servers[pair.Key] = pair.Value;
            }

            return new McpServers
            {
                Servers = servers
            };
        }
    }

    /// <summary>
    /// One configured MCP server, stored generically: the connection kind plus its raw JSON
    /// configuration, which <see cref="HttpMcpServerParameters"/> or
    /// <see cref="StdioMcpServerParameters"/> then decode depending on <see cref="Type"/>.
    /// </summary>
    public class McpServer : ICloneable
    {
        /// <summary>How this server is reached - stdio process or HTTP endpoint - which determines how <see cref="JsonConfiguration"/> is decoded.</summary>
        [JsonPropertyName("Type")]
        public McpServerType Type
        {
            get;
            set;
        }

        /// <summary>The raw, transport-specific configuration JSON, decoded via <see cref="HttpMcpServerParameters"/> or <see cref="StdioMcpServerParameters"/> depending on <see cref="Type"/>.</summary>
        [JsonPropertyName("JsonConfiguration")]
        public string JsonConfiguration
        {
            get;
            set;
        }

        /// <summary>Creates a server entry with an empty configuration.</summary>
        public McpServer()
        {
            JsonConfiguration = string.Empty;
        }

        /// <summary>Creates a server entry of the given transport <paramref name="type"/> with its raw <paramref name="jsonConfiguration"/>.</summary>
        public McpServer(
            McpServerType type,
            string jsonConfiguration
            )
        {
            Type = type;
            JsonConfiguration = jsonConfiguration;
        }

        /// <summary>Whether this server talks HTTP and its endpoint matches <paramref name="endpoint"/> - used to detect a duplicate before adding a new one.</summary>
        public bool IsHttpAndHasEndpoint(string endpoint)
        {
            if (Type != McpServerType.Http)
            {
                return false;
            }

            var mcpServerParameters = HttpMcpServerParameters.DeserializeStdio(this);
            return mcpServerParameters.Endpoint == endpoint;
        }

        public object Clone()
        {
            return new McpServer
            {
                Type = Type,
                JsonConfiguration = JsonConfiguration
            };
        }
    }

    /// <summary>The decoded `JsonConfiguration` of an HTTP-transport <see cref="McpServer"/>: just an endpoint URL.</summary>
    public sealed class HttpMcpServerParameters
    {
        /// <summary>The HTTP URL the MCP server listens on.</summary>
        [JsonPropertyName("url")]
        public string Endpoint
        {
            get; set;
        }

        /// <summary>Creates parameters with an empty endpoint.</summary>
        public HttpMcpServerParameters()
        {
            Endpoint = string.Empty;
        }

        /// <summary>Parses an HTTP server's <see cref="McpServer.JsonConfiguration"/>; throws if <paramref name="server"/> is not <see cref="McpServerType.Http"/>.</summary>
        public static HttpMcpServerParameters DeserializeStdio(
            McpServer server
            )
        {
            if (server.Type != McpServerType.Http)
            {
                throw new InvalidOperationException($"Unsupported type: {server.Type}");
            }

            return System.Text.Json.JsonSerializer.Deserialize<HttpMcpServerParameters>(
                server.JsonConfiguration
                )!;
        }
    }

    /// <summary>
    /// The decoded `JsonConfiguration` of a stdio-transport <see cref="McpServer"/>: the process to
    /// launch, its command-line arguments and the environment variables to set for it.
    /// </summary>
    public sealed class StdioMcpServerParameters
    {
        /// <summary>The executable or command to launch the server process.</summary>
        [JsonPropertyName("command")]
        public string Command
        {
            get; set;
        }

        /// <summary>Command-line arguments passed to <see cref="Command"/> when launching the server process.</summary>
        [JsonPropertyName("args")]
        public string[]? Args
        {
            get; set;
        }

        /// <summary>Environment variables set for the server process, such as API keys or tokens the server itself needs.</summary>
        [JsonPropertyName("env")]
        public Dictionary<string, string>? Env
        {
            get; set;
        }

        /// <summary>Creates parameters with an empty command, args and environment.</summary>
        public StdioMcpServerParameters()
        {
            Command = string.Empty;
            Args = [];
            Env = [];
        }

        /// <summary><see cref="Args"/> re-serialized to JSON, for display in the server-configuration UI.</summary>
        public string GetArgStringRepresentation()
        {
            return JsonSerializer.Serialize(Args ?? []);
        }

        /// <summary><see cref="Env"/> re-serialized to JSON, for display in the server-configuration UI.</summary>
        public string GetEnvStringRepresentation()
        {
            return JsonSerializer.Serialize(Env ?? new Dictionary<string, string>());
        }

        /// <summary>Parses a stdio server's <see cref="McpServer.JsonConfiguration"/>; throws if <paramref name="server"/> is not <see cref="McpServerType.Stdio"/>.</summary>
        public static StdioMcpServerParameters DeserializeStdio(
            McpServer server
            )
        {
            if (server.Type != McpServerType.Stdio)
            {
                throw new InvalidOperationException($"Unsupported type: {server.Type}");
            }

            return System.Text.Json.JsonSerializer.Deserialize<StdioMcpServerParameters>(
                server.JsonConfiguration
                )!;
        }
    }

    /// <summary>How an MCP server is reached: a spawned local process, or an HTTP endpoint.</summary>
    public enum McpServerType
    {
        /// <summary>The server is a local process launched and talked to over standard input/output.</summary>
        Stdio = 0,
        /// <summary>The server is reached over an HTTP endpoint.</summary>
        Http = 1
    }
}
