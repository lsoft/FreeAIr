#nullable disable
#pragma warning disable IDE1006
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dto
{
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

        public string GetArgStringRepresentation()
        {
            return JsonSerializer.Serialize(Args ?? []);
        }

        public string GetEnvStringRepresentation()
        {
            return JsonSerializer.Serialize(Env ?? new Dictionary<string, string>());
        }

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

    public enum McpServerType
    {
        Stdio = 0,
        Http = 1
    }
}
