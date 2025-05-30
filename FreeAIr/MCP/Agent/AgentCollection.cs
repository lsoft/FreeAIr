using FreeAIr.MCP.Agent.External;
using FreeAIr.MCP.Agent.Github;
using FreeAIr.MCP.Agent.VS;
using FreeAIr.Shared.Helper;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
            await ProcessAgentAsync(VisualStudioAgent.Instance);
            await ProcessAgentAsync(GithubAgent.Instance);

            if (ExternalAgentJsonParser.TryParse(out var mcpServers))
            {
                foreach (var mcpServer in mcpServers.Servers)
                {
                    var server = new ExternalAgent(
                        mcpServer.Key,
                        mcpServer.Value
                        );

                    await ProcessAgentAsync(server);
                }
            }
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
                agentWrapper.Tools.Tools.ConvertAll(t => t.ToolName)
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
                            AvailableToolController.GetToolStatus(agentName, tool.ToolName)
                            )
                        );
                }

                result.AddAgent(agent);
            }

            return result;
        }

        public static async Task<string[]?> CallToolAsync(
            string toolName,
            Dictionary<string, object?> arguments,
            CancellationToken cancellationToken
            )
        {
            foreach (var agent in _agentWrappers)
            {
                foreach (var tool in agent.Tools.Tools)
                {
                    if (StringComparer.InvariantCultureIgnoreCase.Compare(tool.FullName, toolName) == 0)
                    {
                        var toolResult = await agent.Agent.CallToolAsync(
                            tool.ToolName,
                            arguments,
                            cancellationToken
                            );
                        if (toolResult is null)
                        {
                            return [];
                        }

                        return toolResult.Content;
                    }
                }
            }

            return null;
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


            private AgentWrapper(
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

        public IReadOnlyList<AgentToolStatus> GetActiveToolList()
        {
            var result = new List<AgentToolStatus>();

            foreach (var agent in this.Agents)
            {
                foreach (var tool in agent.Tools)
                {
                    if (tool.Enabled)
                    {
                        result.Add(tool);
                    }
                }
            }

            return result;
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

        public ChatTool CreateChatTool()
        {
            return Tool.CreateChatTool();
        }
    }

}
