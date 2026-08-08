using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.Options2.Mcp
{
    /// <summary>
    /// Which MCP tools the user has switched on, as the settings file holds them.
    ///
    /// This is the persistent half of the tool state: the servers themselves are discovered at
    /// runtime by the proxy, and this list only remembers the user's decision about each of their
    /// tools. A chat copies it once at creation into its own AvailableToolContainer, so toggling a
    /// tool later does not change a dialogue already under way.
    /// </summary>
    public sealed class AvailableMcpServersJson : ICloneable
    {
        /// <summary>The known servers, each with the on/off state of its tools.</summary>
        public List<AvailableMcpServerJson> Servers
        {
            get;
            set;
        }

        /// <summary>Starts with no servers, which is what a fresh installation shows before any MCP server is configured.</summary>
        public AvailableMcpServersJson()
        {
            Servers = [];
        }

        /// <summary>Deep copy of the server list, used before edits so a cancelled settings dialog leaves the original untouched.</summary>
        public object Clone()
        {
            return new AvailableMcpServersJson
            {
                Servers = Servers.ConvertAll(s => (AvailableMcpServerJson)s.Clone())
            };
        }

        /// <summary>Records a newly discovered server. No check for duplicates — the callers reconcile first.</summary>
        public void Add(AvailableMcpServerJson server)
        {
            if (server is null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            Servers.Add(server);
        }

        /// <summary>
        /// Forgets a server entirely, returning whether anything was actually removed. Called when a
        /// server disappears from the configuration, so its stale tool switches do not linger in the
        /// settings file forever.
        /// </summary>
        public bool DeleteAllToolsForServer(string serverName)
        {
            var lengthBeforeDelete = Servers.Count;
            Servers.RemoveAll(a => a.Name == serverName);
            return Servers.Count != lengthBeforeDelete;
        }
    }

    /// <summary>
    /// One MCP server and the state of every tool it offers. Matched to the running server by name,
    /// which is why renaming a server in the configuration loses its switches.
    /// </summary>
    public sealed class AvailableMcpServerJson : ICloneable
    {
        /// <summary>Name of the server, as written in the MCP configuration. The key everything is matched by.</summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>Its tools, each remembering whether the user left it enabled.</summary>
        public List<AvailableMcpServerToolJson> Tools
        {
            get;
            set;
        }

        /// <summary>Unnamed, with no tools, for the json deserializer.</summary>
        public AvailableMcpServerJson()
        {
            Name = string.Empty;
            Tools = [];
        }

        /// <summary>Deep copy of this server's name and tool switches.</summary>
        public object Clone()
        {
            return new AvailableMcpServerJson
            {
                Name = Name,
                Tools = Tools.ConvertAll(t => (AvailableMcpServerToolJson)t.Clone())
            };
        }

        /// <summary>Builds an entry for a newly discovered server, with every one of its tools enabled by default.</summary>
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

        /// <summary>
        /// Reconciles the stored list with what the server actually offers now: tools which are gone
        /// are dropped, tools already known are switched back on and new ones are added enabled.
        ///
        /// New tools default to enabled because a server is added in order to be used; the price is
        /// that a tool appearing after an update starts working without being asked about.
        /// </summary>
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

        /// <summary>Switches every tool of this server at once — the checkbox on the server row in the tool window.</summary>
        public void UpdateAllTools(bool enabled)
        {
            Tools.ForEach(t => t.Enabled = enabled);
        }
    }

    /// <summary>
    /// One tool and whether the model may call it. This is the switch the user actually flips, and
    /// the only thing that decides whether the tool is offered in the completion request.
    /// </summary>
    public sealed class AvailableMcpServerToolJson : ICloneable
    {
        /// <summary>False hides the tool from the model entirely; it is not offered, not just refused.</summary>
        public bool Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// Name of the tool as the server reports it. This is what the model is shown and what it
        /// names when it asks for a call.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Parameterless and disabled, for the json deserializer. Every other constructor here
        /// enables the tool, so a tool built in code is usable and a tool read from the settings
        /// keeps whatever was written there.
        /// </summary>
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

        /// <summary>Builds a tool switch with an explicit initial state, used when reconciling with what the settings file already recorded.</summary>
        public AvailableMcpServerToolJson(string name, bool enabled)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Enabled = enabled;
        }

        /// <summary>Deep copy of this tool's name and enabled state.</summary>
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
