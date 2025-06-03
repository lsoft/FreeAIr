using Dto;
using System.Text.Json;

namespace FreeAIr.MCP.Agent.External
{
    public static class ExternalAgentJsonParser
    {
        public static bool TryParse(
            string externalMCPServersJson,
            out McpServers? servers
            )
        {
            try
            {
                servers = JsonSerializer.Deserialize<McpServers>(externalMCPServersJson);
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
