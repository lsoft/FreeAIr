using Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.External
{
    public static class ExternalAgentJsonParser
    {
        public static bool TryParse(
            out McpServers? servers
            )
        {
            try
            {
                servers = JsonSerializer.Deserialize<McpServers>(MCPPage.Instance.ExternalMCPServers);
                return true;
            }
            catch
            {
                //error in json
                //todo log
            }

            servers = null;
            return false;
        }
    }
}
