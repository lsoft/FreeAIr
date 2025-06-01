using Dto;
using FreeAIr.MCP.Agent.External;
using FreeAIr.MCP.Agent.Github;
using FreeAIr.MCP.Agent.VS;
using FreeAIr.Shared.Helper;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Linq;
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

        public static async Task<AgentSetupConfigurationResult> SetupConfigurationAsync(
            AvailableToolContainer toolContainer,
            McpServers mcpServers
            )
        {
            if (toolContainer is null)
            {
                throw new ArgumentNullException(nameof(toolContainer));
            }

            if (mcpServers is null)
            {
                throw new ArgumentNullException(nameof(mcpServers));
            }

            var agentToProcess = new List<string>
            {
                VisualStudioAgent.Instance.Name,
                GithubAgent.Instance.Name
            };
            mcpServers.Servers.ForEach(s => agentToProcess.Add(s.Key));

            var successStartedAgents = await ProcessAgentsAsync(
                toolContainer,
                agentToProcess,
                mcpServers
                );
            
            return new AgentSetupConfigurationResult(toolContainer, successStartedAgents);
        }

        private static async Task<List<IAgent>> ProcessAgentsAsync(
            AvailableToolContainer toolContainer,
            List<string> agentToProcess,
            McpServers mcpServers
            )
        {
            var knownAgents = _agentWrappers.ConvertAll(a => a.Agent.Name);
            var deletedAgents = knownAgents.Except(agentToProcess).ToList();
            //var addedAgents = agentToProcess.Except(knownAgents).ToList();
            var addedOrUpdatedAgents = agentToProcess.Except(deletedAgents).ToList();

            DeleteAgents(toolContainer, deletedAgents);
            var successStartedAgents = await AddOrUpdateAgentsAsync(
                toolContainer,
                addedOrUpdatedAgents,
                mcpServers
                );

            return successStartedAgents;
        }

        private static async Task<List<IAgent>> AddOrUpdateAgentsAsync(
            AvailableToolContainer toolContainer,
            List<string> addedOrUpdatedAgents,
            McpServers mcpServers
            )
        {
            if (toolContainer is null)
            {
                throw new ArgumentNullException(nameof(toolContainer));
            }

            if (addedOrUpdatedAgents is null)
            {
                throw new ArgumentNullException(nameof(addedOrUpdatedAgents));
            }

            if (mcpServers is null)
            {
                throw new ArgumentNullException(nameof(mcpServers));
            }

            var successStartedAgents = new List<IAgent>();

            foreach (var addedOrUpdatedAgent in addedOrUpdatedAgents)
            {
                if (addedOrUpdatedAgent == VisualStudioAgent.Instance.Name)
                {
                    if (await ProcessAgentAsync(toolContainer, VisualStudioAgent.Instance))
                    {
                        successStartedAgents.Add(VisualStudioAgent.Instance);
                    }
                }
                else if (addedOrUpdatedAgent == GithubAgent.Instance.Name)
                {
                    if (await ProcessAgentAsync(toolContainer, GithubAgent.Instance))
                    {
                        successStartedAgents.Add(GithubAgent.Instance);
                    }
                }
                else
                {
                    //this is an external agent

                    var mcpServer = mcpServers.Servers.First(s => s.Key == addedOrUpdatedAgent);

                    var agent = new ExternalAgent(
                        mcpServer.Key,
                        mcpServer.Value
                        );
                    if (await ProcessAgentAsync(toolContainer, agent))
                    {
                        successStartedAgents.Add(agent);
                    }
                }
            }

            return successStartedAgents;
        }

        private static void DeleteAgents(
            AvailableToolContainer toolContainer,
            List<string> deletedAgents
            )
        {
            _agentWrappers.RemoveAll(a => a.Agent.Name.In(deletedAgents));
            toolContainer.DeleteAllToolsForAgents(deletedAgents);
        }

        public static async Task<bool> ProcessAgentAsync(
            AvailableToolContainer toolContainer,
            IAgent agent
            )
        {
            if (toolContainer is null)
            {
                throw new ArgumentNullException(nameof(toolContainer));
            }

            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            var agentWrapper = await AgentWrapper.CreateAsync(agent);
            if (agentWrapper is null)
            {
                //агент не запущен, выключаем его тулзы
                toolContainer.DeleteAllToolsForAgents(
                    [agent.Name]
                    );
                return false;
            }

            _agentWrappers.Add(agentWrapper);

            //фиксируем тулзы в настройках (некоторые могли удалиться, некоторые - добавиться)
            //остальные просто включатся
            toolContainer.AddToolsIfNotExists(
                agentWrapper.Agent.Name,
                agentWrapper.Tools.Tools.ConvertAll(t => t.ToolName)
                );

            return true;
        }

        public static AgentsToolsStatusCollection GetTools(
            AvailableToolContainer toolContainer
            )
        {
            if (toolContainer is null)
            {
                throw new ArgumentNullException(nameof(toolContainer));
            }

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
                            toolContainer.GetToolStatus(agentName, tool.ToolName)
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

    public sealed class AgentSetupConfigurationResult
    {
        public AvailableToolContainer ToolContainer
        {
            get;
        }
        public List<IAgent> SuccessStartedAgents
        {
            get;
        }

        public AgentSetupConfigurationResult(
            AvailableToolContainer toolContainer,
            List<IAgent> successStartedAgents
            )
        {
            if (toolContainer is null)
            {
                throw new ArgumentNullException(nameof(toolContainer));
            }

            if (successStartedAgents is null)
            {
                throw new ArgumentNullException(nameof(successStartedAgents));
            }

            ToolContainer = toolContainer;
            SuccessStartedAgents = successStartedAgents;
        }

    }
}
