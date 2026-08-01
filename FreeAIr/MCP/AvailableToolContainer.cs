using FreeAIr.Options2;
using FreeAIr.Options2.Mcp;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.MCP.McpServerProxy
{
    /// <summary>
    /// The tools that are enabled by the settings (Тулзы, доступные согласно настройкам).
    ///
    /// This is a switchboard, not a catalogue: it stores an on/off flag per server/tool pair and
    /// knows nothing about what those tools actually do. The catalogue itself belongs to
    /// <see cref="McpServerProxyCollection"/>, which joins the two in
    /// <see cref="McpServerProxyCollection.GetTools"/>.
    ///
    /// Two kinds of containers exist. The global one is read from and saved to the settings; each
    /// chat gets its own copy at creation time (<see cref="FreeAIr.Chat.Chat.ChatTools"/>), so toggling a tool
    /// inside a chat affects that chat only. A tool which is not mentioned at all counts as
    /// disabled.
    /// </summary>
    public sealed class AvailableToolContainer
    {
        private readonly AvailableMcpServersJson _servers;

        /// <summary>
        /// Reads the global container from the active FreeAIr settings.
        /// </summary>
        public static async System.Threading.Tasks.Task<AvailableToolContainer> ReadSystemAsync()
        {
            var tools = await FreeAIrOptions.DeserializeAvailableToolsAsync();
            var c = new AvailableToolContainer(tools);
            return c;
        }

        /// <summary>
        /// Reads a container from a json text rather than from the active settings. Used by the
        /// Control Center, which edits a settings document the user has not saved yet.
        /// </summary>
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


        /// <summary>
        /// Reconciles the stored tool list of a server with the tools it has just published:
        /// tools that no longer exist are dropped, and the remaining ones end up enabled.
        ///
        /// Beware: the reconciliation switches the surviving tools ON, it does not preserve their
        /// previous flags. This is harmless only as long as the container being reconciled is a
        /// throwaway one and is not saved back into the settings.
        /// </summary>
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
