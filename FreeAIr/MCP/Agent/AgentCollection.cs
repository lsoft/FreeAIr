using FreeAIr.MCP.Agent.Github;
using FreeAIr.MCP.Agent.Github.BLO;
using FreeAIr.Shared.Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FreeAIr.MCP.Agent
{
    /// <summary>
    /// Коллекция текущих инициализированных агентов.
    /// </summary>
    public static class AgentCollection
    {
        private static readonly List<AgentWrapper> _agentWrappers = new();

        public static async Task InitAsync()
        {
            await ProcessAgentAsync(GithubAgent.Instance);
        }

        public static async Task ProcessAgentAsync(
            IAgent agent
            )
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            var agentWrapper = await AgentWrapper.CreateAsync(agent);
            if (agentWrapper is null)
            {
                //агент не запущен, выключаем его тулзы
                AvailableToolController.DeleteAllToolsForAgent(
                    agent.Name
                    );
                return;
            }

            _agentWrappers.Add(agentWrapper);

            //фиксируем тулзы в настройках (некоторые могли удалиться, некоторые - добавиться)
            //остальные просто включатся
            AvailableToolController.AddToolsIfNotExists(
                agentWrapper.Agent.Name,
                agentWrapper.Tools.Tools.ConvertAll(t => t.Name)
                );
        }

        public static AgentsToolsStatusCollection GetTools()
        {
            var result = new AgentsToolsStatusCollection();

            foreach (var agentWrapper in _agentWrappers)
            {
                var agentName = agentWrapper.Agent.Name;
                var agent = new AgentToolsStatus(agentName);

                var agentTools = agentWrapper.Tools;

                foreach (var tool in agentTools.Tools)
                {
                    agent.AddTool(
                        new AgentToolStatus(
                            tool,
                            AvailableToolController.GetToolStatus(agentName, tool.Name)
                            )
                        );
                }

                result.AddAgent(agent);
            }

            return result;
        }


        public sealed class AgentWrapper
        {
            public IAgent Agent
            {
                get;
            }

            public AgentTools Tools
            {
                get;
            }


            public AgentWrapper(
                IAgent agent,
                AgentTools tools
                )
            {
                Agent = agent;
                Tools = tools;
            }

            public static async Task<AgentWrapper?> CreateAsync(
                IAgent agent
                )
            {
                if (agent is null)
                {
                    throw new ArgumentNullException(nameof(agent));
                }

                var isInstalled = await agent.IsInstalledAsync();
                if (!isInstalled)
                {
                    return null;
                }

                var tools = await agent.GetToolsAsync();
                return new AgentWrapper(agent, tools);
            }

        }
    }

    public sealed class AgentsToolsStatusCollection
    {
        private readonly List<AgentToolsStatus> _agents;

        public IReadOnlyList<AgentToolsStatus> Agents => _agents;

        public AgentsToolsStatusCollection()
        {
            _agents = new();
        }

        public void AddAgent(AgentToolsStatus agent)
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            _agents.Add(agent);
        }
    }

    public sealed class AgentToolsStatus
    {
        private readonly List<AgentToolStatus> _tools;

        public string AgentName
        {
            get;
        }

        public IReadOnlyList<AgentToolStatus> Tools => _tools;

        public AgentToolsStatus(string agentName)
        {
            if (agentName is null)
            {
                throw new ArgumentNullException(nameof(agentName));
            }

            AgentName = agentName;
            _tools = new();
        }

        public void AddTool(
            AgentToolStatus tool
            )
        {
            if (tool is null)
            {
                throw new ArgumentNullException(nameof(tool));
            }

            _tools.Add(tool);
        }
    }

    public sealed class AgentToolStatus
    {
        public AgentTool Tool
        {
            get;
        }

        public bool Enabled
        {
            get;
        }

        public AgentToolStatus(
            AgentTool tool,
            bool enabled
            )
        {
            if (tool is null)
            {
                throw new ArgumentNullException(nameof(tool));
            }

            Tool = tool;
            Enabled = enabled;
        }
    }

}
