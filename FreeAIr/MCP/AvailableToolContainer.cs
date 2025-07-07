using FreeAIr.Options2;
using FreeAIr.Options2.Mcp;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.MCP.McpServerProxy
{
    /// <summary>
    /// Тулзы, доступные согласно настройкам.
    /// По существу, парсер настроек Visual Studio.
    /// </summary>
    public sealed class AvailableToolContainer
    {
        private readonly AvailableMcpServersJson _servers;

        public static async System.Threading.Tasks.Task<AvailableToolContainer> ReadSystemAsync()
        {
            var tools = await FreeAIrOptions.DeserializeAvailableToolsAsync();
            var c = new AvailableToolContainer(tools);
            return c;
        }

        public static AvailableToolContainer ReadFromOptions(
            string optionsJson
            )
        {
            var options = FreeAIrOptions.DeserializeFromString(optionsJson);
            var c = new AvailableToolContainer(options.AvailableTools);
            return c;
        }

        private AvailableToolContainer(
            AvailableMcpServersJson servers
            )
        {
            if (servers is null)
            {
                throw new ArgumentNullException(nameof(servers));
            }

            _servers = servers;
        }

        public bool GetToolStatus(
            string serverName,
            string toolName
            )
        {
            var server = _servers.Servers.FirstOrDefault(s => s.Name == serverName);
            if (server is null)
            {
                return false;
            }

            var tool = server.Tools.FirstOrDefault(t => t.Name == toolName);
            if (tool is null)
            {
                return false;
            }

            return tool.Enabled;
        }


        public void AddServer(string serverName)
        {
            _servers.Servers.Add(new AvailableMcpServerJson(serverName, []));
        }

        public void DeleteServer(string serverName)
        {
            _servers.Servers.RemoveAll(a => a.Name == serverName);
        }


        public void AddToolsIfNotExists(
            string serverName,
            IReadOnlyList<string> toolNames
            )
        {
            var server = _servers.Servers.FirstOrDefault(s => s.Name == serverName);
            if (server is null)
            {
                server = new AvailableMcpServerJson(
                    serverName,
                    toolNames
                    );
                _servers.Add(server);
                return;
            }

            server.AddToolsIfNotExists(toolNames);
        }

        public bool DeleteAllToolsForMcpServerProxies(
            IEnumerable<string> serverNames
            )
        {
            if (serverNames is null)
            {
                throw new ArgumentNullException(nameof(serverNames));
            }

            var deleted = false;
            foreach (var serverName in serverNames)
            {
                if (_servers.DeleteAllToolsForServer(serverName))
                {
                    deleted = true;
                }
            }

            return deleted;
        }

        public void AddTool(
            string serverName,
            string toolName,
            bool enabled
            )
        {
            var server = _servers.Servers.FirstOrDefault(s => s.Name == serverName);
            if (server is null)
            {
                return;
            }

            var tool = server.Tools.FirstOrDefault(t => t.Name == toolName);
            if (tool is null)
            {
                server.Tools.Add(new AvailableMcpServerToolJson(toolName, enabled));
                return;
            }

            tool.Enabled = enabled;
        }

        public async Task SaveToSystemAsync()
        {
            await FreeAIrOptions.SaveExternalMCPToolsAsync(
                _servers
                );
        }

        public string SaveTo(
            string optionsJson
            )
        {
            var options = FreeAIrOptions.DeserializeFromString(optionsJson);
            options.AvailableTools = this._servers;
            return FreeAIrOptions.SerializeToString(options);
        }
    }
}
