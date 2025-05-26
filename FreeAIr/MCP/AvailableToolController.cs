using FreeAIr.Shared.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace FreeAIr.MCP.Agent
{
    /// <summary>
    /// Тулзы, доступные согласно настройкам.
    /// По существу, парсер настроек Visual Studio.
    /// </summary>
    public static class AvailableToolController
    {
        public static bool GetToolStatus(
            string agentName,
            string toolName
            )
        {
            var agents = Read();
            var agent = agents.Agents.FirstOrDefault(s => s.Name == agentName);
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

        public static void AddToolsIfNotExists(
            string agentName,
            IReadOnlyList<string> toolNames
            )
        {
            var agents = Read();
            var agent = agents.Agents.FirstOrDefault(s => s.Name == agentName);
            if (agent is null)
            {
                agent = new AvailableAgent(
                    agentName,
                    toolNames
                    );
                agents.Add(agent);
                Save(agents);
                return;
            }

            agent.AddToolsIfNotExists(toolNames);
            Save(agents);
        }

        public static void DeleteAllToolsForAgent(
            string agentName
            )
        {
            var agents = Read();
            
            var deleted = agents.DeleteAllToolsForAgent(agentName);
            if (!deleted)
            {
                return;
            }

            Save(agents);
        }

        public static void UpdateTool(
            string agentName,
            string toolName,
            bool enabled
            )
        {
            var agents = Read();
            var agent = agents.Agents.FirstOrDefault(s => s.Name == agentName);
            if (agent is null)
            {
                return;
            }

            var tool = agent.Tools.FirstOrDefault(t => t.Name == toolName);
            if (tool is null)
            {
                return;
            }

            tool.Enabled = enabled;
            Save(agents);
        }


        private static AvailableAgents Read()
        {
            var json = InternalPage.Instance.AvailableTools;
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

        public static void Save(AvailableAgents agents)
        {
            if (agents is null)
            {
                throw new ArgumentNullException(nameof(agents));
            }

            InternalPage.Instance.AvailableTools = JsonSerializer.Serialize(agents);
            InternalPage.Instance.Save();
        }


        public sealed class AvailableAgents
        {
            public AvailableAgent[] Agents
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

                Agents = Agents.Append(agent).ToArray();
            }

            public bool DeleteAllToolsForAgent(string agentName)
            {
                var lengthBeforeDelete = Agents.Length;
                Agents = Agents.RemoveAll(a => a.Name == agentName).ToArray();
                return Agents.Length != lengthBeforeDelete;
            }
        }

        public sealed class AvailableAgent
        {
            public string Name
            {
                get;
                set;
            }

            public AvailableAgentTool[] Tools
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
                Tools = toolNames.ConvertAll(t => new AvailableAgentTool(t)).ToArray();
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

                Tools = tools.ToArray();
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

        }
    }
}
