using Dto;
using FreeAIr.MCP.McpServerProxy.External;
using FreeAIr.MCP.McpServerProxy.Github;
using FreeAIr.MCP.McpServerProxy.VS;
using FreeAIr.Shared.Helper;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy
{

    /// <summary>
    /// Коллекция текущих инициализированных MCP servers.
    /// </summary>
    public static class McpServerProxyCollection
    {
        private static readonly List<McpServerProxyWrapper> _mcpServerProxyWrappers = new();

        public static async Task<McpServersSetupConfigurationResult> SetupConfigurationAsync(
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

            var mcpServerProxiesToProcess = new List<string>
            {
                VisualStudioMcpServerProxy.Instance.Name,
                GithubMcpServerProxy.Instance.Name
            };
            mcpServers.Servers.ForEach(s => mcpServerProxiesToProcess.Add(s.Key));

            var successStartedMcpServerProxies = await ProcessMcpServerProxiesAsync(
                toolContainer,
                mcpServerProxiesToProcess,
                mcpServers
                );
            
            return new McpServersSetupConfigurationResult(toolContainer, successStartedMcpServerProxies);
        }

        private static async Task<List<IMcpServerProxy>> ProcessMcpServerProxiesAsync(
            AvailableToolContainer toolContainer,
            List<string> mcpServerProxiesToProcess,
            McpServers mcpServers
            )
        {
            var known = _mcpServerProxyWrappers.ConvertAll(a => a.McpServerProxy.Name);
            var deleted = known.Except(mcpServerProxiesToProcess).ToList();
            var addedOrUpdated = mcpServerProxiesToProcess.Except(deleted).ToList();

            DeleteMcpServerProxies(toolContainer, deleted);
            var successStarted = await AddOrUpdateMcpServerProxiesAsync(
                toolContainer,
                addedOrUpdated,
                mcpServers
                );

            return successStarted;
        }

        private static async Task<List<IMcpServerProxy>> AddOrUpdateMcpServerProxiesAsync(
            AvailableToolContainer toolContainer,
            List<string> addedOrUpdatedMcpServerProxies,
            McpServers mcpServers
            )
        {
            if (toolContainer is null)
            {
                throw new ArgumentNullException(nameof(toolContainer));
            }

            if (addedOrUpdatedMcpServerProxies is null)
            {
                throw new ArgumentNullException(nameof(addedOrUpdatedMcpServerProxies));
            }

            if (mcpServers is null)
            {
                throw new ArgumentNullException(nameof(mcpServers));
            }

            var successStarted = new List<IMcpServerProxy>();

            foreach (var addedOrUpdated in addedOrUpdatedMcpServerProxies)
            {
                if (addedOrUpdated == VisualStudioMcpServerProxy.Instance.Name)
                {
                    if (await ProcessMcpServerProxyAsync(toolContainer, VisualStudioMcpServerProxy.Instance))
                    {
                        successStarted.Add(VisualStudioMcpServerProxy.Instance);
                    }
                }
                else if (addedOrUpdated == GithubMcpServerProxy.Instance.Name)
                {
                    if (await ProcessMcpServerProxyAsync(toolContainer, GithubMcpServerProxy.Instance))
                    {
                        successStarted.Add(GithubMcpServerProxy.Instance);
                    }
                }
                else
                {
                    //this is an external McpServerProxy

                    var mcpServer = mcpServers.Servers.First(s => s.Key == addedOrUpdated);

                    var mcpServerProxy = new ExternalMcpServerProxy(
                        mcpServer.Key
                        //mcpServer.Value
                        );
                    if (await ProcessMcpServerProxyAsync(toolContainer, mcpServerProxy))
                    {
                        successStarted.Add(mcpServerProxy);
                    }
                }
            }

            return successStarted;
        }

        private static void DeleteMcpServerProxies(
            AvailableToolContainer toolContainer,
            List<string> deletedMcpServerProxies
            )
        {
            _mcpServerProxyWrappers.RemoveAll(a => a.McpServerProxy.Name.In(deletedMcpServerProxies));
            toolContainer.DeleteAllToolsForMcpServerProxies(deletedMcpServerProxies);
        }

        public static async Task<bool> ProcessMcpServerProxyAsync(
            AvailableToolContainer toolContainer,
            IMcpServerProxy mcpServerProxy
            )
        {
            if (toolContainer is null)
            {
                throw new ArgumentNullException(nameof(toolContainer));
            }

            if (mcpServerProxy is null)
            {
                throw new ArgumentNullException(nameof(mcpServerProxy));
            }

            var mcpServerProxyWrapper = await McpServerProxyWrapper.CreateAsync(mcpServerProxy);
            if (mcpServerProxyWrapper is null)
            {
                //агент не запущен, выключаем его тулзы
                toolContainer.DeleteAllToolsForMcpServerProxies(
                    [mcpServerProxy.Name]
                    );
                return false;
            }

            _mcpServerProxyWrappers.Add(mcpServerProxyWrapper);

            //фиксируем тулзы в настройках (некоторые могли удалиться, некоторые - добавиться)
            //остальные просто включатся
            toolContainer.AddToolsIfNotExists(
                mcpServerProxyWrapper.McpServerProxy.Name,
                mcpServerProxyWrapper.Tools.Tools.ConvertAll(t => t.ToolName)
                );

            return true;
        }

        public static McpServerProxiesToolsStatusCollection GetTools(
            AvailableToolContainer toolContainer
            )
        {
            if (toolContainer is null)
            {
                throw new ArgumentNullException(nameof(toolContainer));
            }

            var result = new McpServerProxiesToolsStatusCollection();

            foreach (var mcpServerProxyWrapper in _mcpServerProxyWrappers)
            {
                var mcpServerProxyName = mcpServerProxyWrapper.McpServerProxy.Name;

                var toolsStatus = new McpServerToolsStatus(mcpServerProxyName);
                foreach (var tool in mcpServerProxyWrapper.Tools.Tools)
                {
                    toolsStatus.AddTool(
                        new McpServerToolStatus(
                            tool,
                            toolContainer.GetToolStatus(mcpServerProxyName, tool.ToolName)
                            )
                        );
                }

                result.AddToolsStatus(toolsStatus);
            }

            return result;
        }

        public static async Task<string[]?> CallToolAsync(
            string toolName,
            Dictionary<string, object?> arguments,
            CancellationToken cancellationToken
            )
        {
            foreach (var mcpServerProxy in _mcpServerProxyWrappers)
            {
                foreach (var tool in mcpServerProxy.Tools.Tools)
                {
                    if (StringComparer.InvariantCultureIgnoreCase.Compare(tool.FullName, toolName) == 0)
                    {
                        var toolResult = await mcpServerProxy.McpServerProxy.CallToolAsync(
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

        public sealed class McpServerProxyWrapper
        {
            public IMcpServerProxy McpServerProxy
            {
                get;
            }

            public McpServerTools Tools
            {
                get;
            }


            private McpServerProxyWrapper(
                IMcpServerProxy mcpServerProxy,
                McpServerTools tools
                )
            {
                McpServerProxy = mcpServerProxy;
                Tools = tools;
            }

            public static async Task<McpServerProxyWrapper?> CreateAsync(
                IMcpServerProxy mcpServerProxy
                )
            {
                if (mcpServerProxy is null)
                {
                    throw new ArgumentNullException(nameof(mcpServerProxy));
                }

                var isInstalled = await mcpServerProxy.IsInstalledAsync();
                if (!isInstalled)
                {
                    return null;
                }

                var tools = await mcpServerProxy.GetToolsAsync();
                return new McpServerProxyWrapper(mcpServerProxy, tools);
            }

        }
    }

    public sealed class McpServerProxiesToolsStatusCollection
    {
        private readonly List<McpServerToolsStatus> _toolsStatuses;

        public IReadOnlyList<McpServerToolsStatus> ToolsStatuses => _toolsStatuses;

        public McpServerProxiesToolsStatusCollection()
        {
            _toolsStatuses = new();
        }

        public void AddToolsStatus(McpServerToolsStatus toolsStatus)
        {
            if (toolsStatus is null)
            {
                throw new ArgumentNullException(nameof(toolsStatus));
            }

            _toolsStatuses.Add(toolsStatus);
        }

        public IReadOnlyList<McpServerToolStatus> GetActiveToolList()
        {
            var result = new List<McpServerToolStatus>();

            foreach (var toolsStatus in ToolsStatuses)
            {
                foreach (var tool in toolsStatus.Tools)
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

    public sealed class McpServerToolsStatus
    {
        private readonly List<McpServerToolStatus> _tools;

        public string McpServerProxyName
        {
            get;
        }

        public IReadOnlyList<McpServerToolStatus> Tools => _tools;

        public McpServerToolsStatus(string mcpServerProxyName)
        {
            if (mcpServerProxyName is null)
            {
                throw new ArgumentNullException(nameof(mcpServerProxyName));
            }

            McpServerProxyName = mcpServerProxyName;
            _tools = new();
        }

        public void AddTool(
            McpServerToolStatus tool
            )
        {
            if (tool is null)
            {
                throw new ArgumentNullException(nameof(tool));
            }

            _tools.Add(tool);
        }
    }

    public sealed class McpServerToolStatus
    {
        public McpServerTool Tool
        {
            get;
        }

        public bool Enabled
        {
            get;
        }

        public McpServerToolStatus(
            McpServerTool tool,
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

    public sealed class McpServersSetupConfigurationResult
    {
        public AvailableToolContainer ToolContainer
        {
            get;
        }
        public List<IMcpServerProxy> SuccessStartedMcpServers
        {
            get;
        }

        public McpServersSetupConfigurationResult(
            AvailableToolContainer toolContainer,
            List<IMcpServerProxy> successStartedMcpServers
            )
        {
            if (toolContainer is null)
            {
                throw new ArgumentNullException(nameof(toolContainer));
            }

            if (successStartedMcpServers is null)
            {
                throw new ArgumentNullException(nameof(successStartedMcpServers));
            }

            ToolContainer = toolContainer;
            SuccessStartedMcpServers = successStartedMcpServers;
        }

    }
}
