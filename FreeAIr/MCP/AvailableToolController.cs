using FreeAIr.Shared.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FreeAIr.MCP.Agent
{
    public static class AvailableToolController
    {
        public static void AddToolsIfNotExists(
            string serverName,
            IReadOnlyList<string> toolNames
            )
        {
            var servers = Read();
            var server = servers.Servers.FirstOrDefault(s => s.Name == serverName);
            if (server is null)
            {
                server = new AvailableServer(
                    serverName,
                    toolNames
                    );
                servers.Add(server);
                Save(servers);
                return;
            }

            server.AddToolsIfNotExists(toolNames);
            Save(servers);
        }

        private static AvailableServers Read()
        {
            var json = MCPPage.Instance.AvailableTools;
            if (string.IsNullOrEmpty(json))
            {
                return new AvailableServers();
            }

            try
            {
                var result = JsonSerializer.Deserialize<AvailableServers>(json);
                return result;
            }
            catch (Exception excp)
            {
                //todo log
            }

            return new AvailableServers();
        }

        public static void Save(AvailableServers servers)
        {
            if (servers is null)
            {
                throw new ArgumentNullException(nameof(servers));
            }

            MCPPage.Instance.AvailableTools = JsonSerializer.Serialize(servers);
            MCPPage.Instance.Save();
        }

        public sealed class AvailableServers
        {
            public AvailableServer[] Servers
            {
                get;
                set;
            }

            public AvailableServers()
            {
                Servers = [];
            }

            public void Add(AvailableServer server)
            {
                if (server is null)
                {
                    throw new ArgumentNullException(nameof(server));
                }

                Servers = Servers.Append(server).ToArray();
            }
        }

        public sealed class AvailableServer
        {
            public string Name
            {
                get;
                set;
            }

            public AvailableServerTool[] Tools
            {
                get;
                set;
            }

            public AvailableServer()
            {
                Name = string.Empty;
                Tools = [];
            }

            public AvailableServer(
                string name,
                IReadOnlyList<string> toolNames
                )
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                Name = name;
                Tools = toolNames.ConvertAll(t => new AvailableServerTool(t)).ToArray();
            }

            public void AddToolsIfNotExists(
                IReadOnlyList<string> toolNames
                )
            {
                var tools = Tools.ToList();
                foreach (var toolName in toolNames)
                {
                    var tool = tools.FirstOrDefault(t => t.Name == toolName); //O(N*N), but this is not a problem: there are max 20-30 tools in the list
                    if (tool is not null)
                    {
                        tool.Enabled = true;
                    }
                    else
                    {
                        tools.Add(new AvailableServerTool(toolName));
                    }
                }

                Tools = tools.ToArray();
            }
        }

        public sealed class AvailableServerTool
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

            public AvailableServerTool()
            {
                Enabled = false;
                Name = string.Empty;
            }

            public AvailableServerTool(string name)
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                Enabled = true;
                Name = name;
            }

        }
    }
}
