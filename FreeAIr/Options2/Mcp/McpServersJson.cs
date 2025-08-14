using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.Options2.Mcp
{
    public sealed class AvailableMcpServersJson : ICloneable
    {
        public List<AvailableMcpServerJson> Servers
        {
            get;
            set;
        }

        public AvailableMcpServersJson()
        {
            Servers = [];
        }

        public object Clone()
        {
            return new AvailableMcpServersJson
            {
                Servers = Servers.ConvertAll(s => (AvailableMcpServerJson)s.Clone())
            };
        }

        public void Add(AvailableMcpServerJson server)
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

    public sealed class AvailableMcpServerJson : ICloneable
    {
        public string Name
        {
            get;
            set;
        }

        public List<AvailableMcpServerToolJson> Tools
        {
            get;
            set;
        }

        public AvailableMcpServerJson()
        {
            Name = string.Empty;
            Tools = [];
        }

        public object Clone()
        {
            return new AvailableMcpServerJson
            {
                Name = Name,
                Tools = Tools.ConvertAll(t => (AvailableMcpServerToolJson)t.Clone())
            };
        }

        public AvailableMcpServerJson(
            string name,
            IReadOnlyList<string> toolNames
            )
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Tools = toolNames.ConvertAll(t => new AvailableMcpServerToolJson(t));
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
                    tools.Add(new AvailableMcpServerToolJson(toolName));
                }
            }

            Tools = tools;
        }

        public void UpdateAllTools(bool enabled)
        {
            Tools.ForEach(t => t.Enabled = enabled);
        }
    }

    public sealed class AvailableMcpServerToolJson : ICloneable
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

        public AvailableMcpServerToolJson()
        {
            Enabled = false;
            Name = string.Empty;
        }

        public AvailableMcpServerToolJson(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Enabled = true;
            Name = name;
        }

        public AvailableMcpServerToolJson(string name, bool enabled)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Enabled = enabled;
        }

        public object Clone()
        {
            return new AvailableMcpServerToolJson
            {
                Enabled = Enabled,
                Name = Name
            };
        }
    }
}
