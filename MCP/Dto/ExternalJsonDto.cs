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
        [JsonPropertyName("mcpServers")]
        public Dictionary<string, McpServer> Servers
        {
            get; set;
        }

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
        [JsonPropertyName("Type")]
        public McpServerType Type
        {
            get;
            set;
        }

        [JsonPropertyName("JsonConfiguration")]
        public string JsonConfiguration
        {
            get;
            set;
        }

        public McpServer()
        {
            JsonConfiguration = string.Empty;
        }

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
        [JsonPropertyName("url")]
        public string Endpoint
        {
            get; set;
        }

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
        [JsonPropertyName("command")]
        public string Command
        {
            get; set;
        }

        [JsonPropertyName("args")]
        public string[]? Args
        {
            get; set;
        }

        [JsonPropertyName("env")]
        public Dictionary<string, string>? Env
        {
            get; set;
        }

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
        Stdio = 0,
        Http = 1
    }
}
