using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FreeAIr.MCP.McpServerProxy
{
    /// <summary>
    /// Тулзы, доступные согласно настройкам.
    /// По существу, парсер настроек Visual Studio.
    /// </summary>
    public sealed class AvailableToolContainer
    {
        private readonly AvailableMcpServers _servers;

        public static AvailableToolContainer ReadSystem() => new AvailableToolContainer();

        private AvailableToolContainer()
        {
            _servers = Read();
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
            _servers.Servers.Add(new AvailableMcpServer(serverName, []));
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
                server = new AvailableMcpServer(
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
                server.Tools.Add(new AvailableMcpServerTool(toolName, enabled));
                return;
            }

            tool.Enabled = enabled;
        }

        public void SaveToSystem()
        {
            MCPPage.Instance.AvailableTools = JsonSerializer.Serialize(_servers);
            MCPPage.Instance.Save();
        }

        private static AvailableMcpServers Read()
        {
            var json = MCPPage.Instance.AvailableTools;
            if (string.IsNullOrEmpty(json))
            {
                return new AvailableMcpServers();
            }

            try
            {
                var result = JsonSerializer.Deserialize<AvailableMcpServers>(json);
                return result;
            }
            catch (Exception excp)
            {
                //todo log
            }

            return new AvailableMcpServers();
        }

        public sealed class AvailableMcpServers
        {
            public List<AvailableMcpServer> Servers
            {
                get;
                set;
            }

            public AvailableMcpServers()
            {
                Servers = [];
            }

            public void Add(AvailableMcpServer server)
            {
                if (server is null)
                {
                    throw new ArgumentNullException(nameof(server));
                }

                Servers.Add(server);
            }

            public bool DeleteAllToolsForServer(string serverName)
            {
                var lengthBeforeDelete = Servers.Count;
                Servers.RemoveAll(a => a.Name == serverName);
                return Servers.Count != lengthBeforeDelete;
            }
        }

        public sealed class AvailableMcpServer
        {
            public string Name
            {
                get;
                set;
            }

            public List<AvailableMcpServerTool> Tools
            {
                get;
                set;
            }

            public AvailableMcpServer()
            {
                Name = string.Empty;
                Tools = [];
            }

            public AvailableMcpServer(
                string name,
                IReadOnlyList<string> toolNames
                )
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                Name = name;
                Tools = toolNames.ConvertAll(t => new AvailableMcpServerTool(t));
            }

            public void AddToolsIfNotExists(
                IReadOnlyList<string> toolNames
                )
            {
                //here is O(N*N), but this is not a problem: there are max 20-30 tools in the list

                var tools = Tools.ToList();
                tools.RemoveAll(t => !toolNames.Contains(t.Name)); //delete tools which is non existent now

                foreach (var toolName in toolNames)
                {
                    var tool = tools.FirstOrDefault(t => t.Name == toolName);
                    if (tool is not null)
                    {
                        tool.Enabled = true;
                    }
                    else
                    {
                        tools.Add(new AvailableMcpServerTool(toolName));
                    }
                }

                Tools = tools;
            }

            public void UpdateAllTools(bool enabled)
            {
                Tools.ForEach(t => t.Enabled = enabled);
            }
        }

        public sealed class AvailableMcpServerTool
        {
            public bool Enabled
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }

            public AvailableMcpServerTool()
            {
                Enabled = false;
                Name = string.Empty;
            }

            public AvailableMcpServerTool(string name)
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                Enabled = true;
                Name = name;
            }

            public AvailableMcpServerTool(string name, bool enabled)
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                Name = name;
                Enabled = enabled;
            }
        }
    }
}
