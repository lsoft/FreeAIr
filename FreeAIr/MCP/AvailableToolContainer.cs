using FreeAIr.MCP.Agent.External;
using FreeAIr.Shared.Helper;
using Microsoft.VisualStudio.Shell.Design;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FreeAIr.MCP.Agent
{
    /// <summary>
    /// Тулзы, доступные согласно настройкам.
    /// По существу, парсер настроек Visual Studio.
    /// </summary>
    public sealed class AvailableToolContainer
    {
        private readonly AvailableAgents _agents;

        public static AvailableToolContainer ReadSystem() => new AvailableToolContainer();

        private AvailableToolContainer()
        {
            _agents = Read();
        }

        public bool GetToolStatus(
            string agentName,
            string toolName
            )
        {
            var agent = _agents.Agents.FirstOrDefault(s => s.Name == agentName);
            if (agent is null)
            {
                return false;
            }

            var tool = agent.Tools.FirstOrDefault(t => t.Name == toolName);
            if (tool is null)
            {
                return false;
            }

            return tool.Enabled;
        }


        public void AddAgent(string agentName)
        {
            _agents.Agents.Add(new AvailableAgent(agentName, []));
        }

        public void DeleteAgent(string agentName)
        {
            _agents.Agents.RemoveAll(a => a.Name == agentName);
        }


        public void AddToolsIfNotExists(
            string agentName,
            IReadOnlyList<string> toolNames
            )
        {
            var agent = _agents.Agents.FirstOrDefault(s => s.Name == agentName);
            if (agent is null)
            {
                agent = new AvailableAgent(
                    agentName,
                    toolNames
                    );
                _agents.Add(agent);
                return;
            }

            agent.AddToolsIfNotExists(toolNames);
        }

        public bool DeleteAllToolsForAgents(
            IEnumerable<string> agentNames
            )
        {
            if (agentNames is null)
            {
                throw new ArgumentNullException(nameof(agentNames));
            }

            var deleted = false;
            foreach (var agentNAme in agentNames)
            {
                if (_agents.DeleteAllToolsForAgent(agentNAme))
                {
                    deleted = true;
                }
            }

            return deleted;
        }

        public void AddTool(
            string agentName,
            string toolName,
            bool enabled
            )
        {
            var agent = _agents.Agents.FirstOrDefault(s => s.Name == agentName);
            if (agent is null)
            {
                return;
            }

            var tool = agent.Tools.FirstOrDefault(t => t.Name == toolName);
            if (tool is null)
            {
                agent.Tools.Add(new AvailableAgentTool(toolName, enabled));
                return;
            }

            tool.Enabled = enabled;
        }

        public void SaveToSystem()
        {
            MCPPage.Instance.AvailableTools = JsonSerializer.Serialize(_agents);
            MCPPage.Instance.Save();
        }

        private static AvailableAgents Read()
        {
            var json = MCPPage.Instance.AvailableTools;
            if (string.IsNullOrEmpty(json))
            {
                return new AvailableAgents();
            }

            try
            {
                var result = JsonSerializer.Deserialize<AvailableAgents>(json);
                return result;
            }
            catch (Exception excp)
            {
                //todo log
            }

            return new AvailableAgents();
        }

        public sealed class AvailableAgents
        {
            public List<AvailableAgent> Agents
            {
                get;
                set;
            }

            public AvailableAgents()
            {
                Agents = [];
            }

            public void Add(AvailableAgent agent)
            {
                if (agent is null)
                {
                    throw new ArgumentNullException(nameof(agent));
                }

                Agents.Add(agent);
            }

            public bool DeleteAllToolsForAgent(string agentName)
            {
                var lengthBeforeDelete = Agents.Count;
                Agents.RemoveAll(a => a.Name == agentName);
                return Agents.Count != lengthBeforeDelete;
            }
        }

        public sealed class AvailableAgent
        {
            public string Name
            {
                get;
                set;
            }

            public List<AvailableAgentTool> Tools
            {
                get;
                set;
            }

            public AvailableAgent()
            {
                Name = string.Empty;
                Tools = [];
            }

            public AvailableAgent(
                string name,
                IReadOnlyList<string> toolNames
                )
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                Name = name;
                Tools = toolNames.ConvertAll(t => new AvailableAgentTool(t));
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
                        tools.Add(new AvailableAgentTool(toolName));
                    }
                }

                Tools = tools;
            }

            public void UpdateAllToolsForAgent(bool enabled)
            {
                Tools.ForEach(t => t.Enabled = enabled);
            }
        }

        public sealed class AvailableAgentTool
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

            public AvailableAgentTool()
            {
                Enabled = false;
                Name = string.Empty;
            }

            public AvailableAgentTool(string name)
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                Enabled = true;
                Name = name;
            }

            public AvailableAgentTool(string name, bool enabled)
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
