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

        public McpServer()
        {
        }

        public object Clone()
        {
            Dictionary<string, string>? env = null;
            if (Env is not null)
            {
                env = new Dictionary<string, string>();
                foreach (var pair in Env)
                {
                    env[pair.Key] = pair.Value;
                }
            }

            return new McpServer
            {
                Command = Command,
                Args = Args,
                Env = env
            };
        }

        public string GetArgStringRepresentation()
        {
            return JsonSerializer.Serialize(Args ?? []);
        }

        public string GetEnvStringRepresentation()
        {
            return JsonSerializer.Serialize(Env ?? new Dictionary<string, string>());
        }
    }
}
