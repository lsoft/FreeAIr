#nullable disable
#pragma warning disable IDE1006
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Dto
{
    public class McpServers
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
    }

    public class McpServer
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
