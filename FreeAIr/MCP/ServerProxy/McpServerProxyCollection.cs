using Dto;
using FreeAIr.MCP.McpServerProxy.External;
using FreeAIr.MCP.McpServerProxy.Github;
using FreeAIr.MCP.McpServerProxy.VS;
using FreeAIr.Shared.Helper;
using FreeAIr.Llm;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy
{

    /// <summary>
    /// The collection of currently initialized MCP servers (Коллекция текущих инициализированных
    /// MCP servers).
    ///
    /// There are three flavours behind the common <see cref="IMcpServerProxy"/> interface:
    /// the built-in Visual Studio server (runs inside devenv and therefore can touch DTE/Roslyn),
    /// the github.com server and any number of user-configured external servers — the latter two
    /// are hosted by `Proxy.exe`.
    ///
    /// This class also owns the tool catalogue: after every reconfiguration the tools of the
    /// started servers are merged into <see cref="AvailableToolContainer"/>, and the tools of the
    /// servers that went away are dropped from it.
    /// </summary>
    public static class McpServerProxyCollection
    {
        /// <summary>The servers currently started, each paired with the tool list it reported. Rebuilt on every call to <see cref="SetupConfigurationAsync"/>.</summary>
        private static readonly List<McpServerProxyWrapper> _mcpServerProxyWrappers = new();

        /// <summary>
        /// Brings the running set of servers in line with the requested one. The two built-in
        /// servers are always in the requested set, so they are (re)started on every call.
        /// </summary>
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

        /// <summary>Splits the requested server names into those to tear down and those to (re)start, then applies both.</summary>
        private static async Task<List<IMcpServerProxy>> ProcessMcpServerProxiesAsync(
            AvailableToolContainer toolContainer,
            List<string> mcpServerProxiesToProcess,
            McpServers mcpServers
            )
        {
            var known = _mcpServerProxyWrappers.ConvertAll(a => a.McpServerProxy.Name);

            //серверы, которых больше не заказывали: их надо погасить
            var deleted = known.Except(mcpServerProxiesToProcess).ToList();

            //все заказанные - и новые, и уже работающие: последние тоже надо перечитать,
            //потому что набор их тулзов мог измениться. Distinct - на случай дублей в конфигурации.
            //Раньше здесь было Except(deleted), что по построению deleted не отсеивало ничего
            var addedOrUpdated = mcpServerProxiesToProcess.Distinct().ToList();

            DeleteMcpServerProxies(toolContainer, deleted);
            var successStarted = await AddOrUpdateMcpServerProxiesAsync(
                toolContainer,
                addedOrUpdated,
                mcpServers
                );

            return successStarted;
        }

        /// <summary>Starts or refreshes each named server - the two built-ins by identity, everything else as an <see cref="ExternalMcpServerProxy"/> - and collects the ones that came up successfully.</summary>
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

        /// <summary>Removes the named servers' wrappers from the running set and drops their tools from the container.</summary>
        private static void DeleteMcpServerProxies(
            AvailableToolContainer toolContainer,
            List<string> deletedMcpServerProxies
            )
        {
            _mcpServerProxyWrappers.RemoveAll(a => a.McpServerProxy.Name.In(deletedMcpServerProxies));
            toolContainer.DeleteAllToolsForMcpServerProxies(deletedMcpServerProxies);
        }

        /// <summary>
        /// Starts a single server and registers its tools.
        /// Returns false when the server is not installed or refused to start; in that case its
        /// tools are removed from the container so they are not offered to the LLM.
        /// </summary>
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

            //этот сервер мог быть запущен раньше; его старая обёртка более не актуальна,
            //иначе её тулзы будут выданы модели ещё раз (и ещё раз на следующем перезапуске)
            _mcpServerProxyWrappers.RemoveAll(w => w.McpServerProxy.Name == mcpServerProxy.Name);

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

        /// <summary>
        /// Joins the tools published by the running servers with their enabled/disabled state
        /// taken from the given container (global one, or a chat-scoped one).
        /// </summary>
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

        /// <summary>
        /// Finds the server which owns the tool and invokes it there.
        /// </summary>
        /// <param name="toolName">
        /// The full name (`&lt;server&gt;.&lt;tool&gt;`) as it was given to the LLM. Matched
        /// case-insensitively, because models are not reliable about the casing they echo back.
        /// </param>
        /// <returns>Null when no running server publishes such a tool.</returns>
        public static async Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            Dictionary<string, object?> arguments,
            CancellationToken cancellationToken
            )
        {
            foreach (var mcpServerProxyWrapper in _mcpServerProxyWrappers)
            {
                foreach (var tool in mcpServerProxyWrapper.Tools.Tools)
                {
                    if (StringComparer.InvariantCultureIgnoreCase.Compare(tool.FullName, toolName) == 0)
                    {
                        var toolResult = await mcpServerProxyWrapper.McpServerProxy.CallToolAsync(
                            tool.ToolName,
                            arguments,
                            cancellationToken
                            );

                        return toolResult;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// A started MCP server paired with the tool list it reported when it came up, so the tool
        /// list does not have to be re-fetched from the server on every use.
        /// </summary>
        public sealed class McpServerProxyWrapper
        {
            /// <summary>The started server this wrapper caches tools for.</summary>
            public IMcpServerProxy McpServerProxy
            {
                get;
            }

            /// <summary>The tools the server reported when it was started.</summary>
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

            /// <summary>
            /// Checks the server is installed, fetches its tool list, and wraps both together;
            /// returns null when the server is not installed so the caller can skip it.
            /// </summary>
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

    /// <summary>
    /// The tool status of every running MCP server, grouped by server, as returned by
    /// <see cref="McpServerProxyCollection.GetTools"/> for display and for building the tool list
    /// actually sent to the chat model.
    /// </summary>
    public sealed class McpServerProxiesToolsStatusCollection
    {
        private readonly List<McpServerToolsStatus> _toolsStatuses;

        /// <summary>The per-server tool status groups collected so far.</summary>
        public IReadOnlyList<McpServerToolsStatus> ToolsStatuses => _toolsStatuses;

        /// <summary>Creates an empty collection to be filled with per-server tool status groups.</summary>
        public McpServerProxiesToolsStatusCollection()
        {
            _toolsStatuses = new();
        }

        /// <summary>Adds one server's tool status group to the collection.</summary>
        public void AddToolsStatus(McpServerToolsStatus toolsStatus)
        {
            if (toolsStatus is null)
            {
                throw new ArgumentNullException(nameof(toolsStatus));
            }

            _toolsStatuses.Add(toolsStatus);
        }

        /// <summary>Flattens every server's tools down to just the ones currently enabled, for sending to the chat model.</summary>
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

    /// <summary>
    /// The enabled/disabled status of every tool published by one MCP server.
    /// </summary>
    public sealed class McpServerToolsStatus
    {
        private readonly List<McpServerToolStatus> _tools;

        /// <summary>The name of the server these tools belong to.</summary>
        public string McpServerProxyName
        {
            get;
        }

        /// <summary>The tools published by this server, each with its enabled/disabled state.</summary>
        public IReadOnlyList<McpServerToolStatus> Tools => _tools;

        /// <summary>Creates an empty status group for the named server.</summary>
        public McpServerToolsStatus(string mcpServerProxyName)
        {
            if (mcpServerProxyName is null)
            {
                throw new ArgumentNullException(nameof(mcpServerProxyName));
            }

            McpServerProxyName = mcpServerProxyName;
            _tools = new();
        }

        /// <summary>Adds one tool's status to this server's group.</summary>
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

    /// <summary>
    /// Pairs a single MCP tool with whether it is currently enabled for use, i.e. whether it will be
    /// offered to the chat model.
    /// </summary>
    public sealed class McpServerToolStatus
    {
        /// <summary>The tool this status describes.</summary>
        public McpServerTool Tool
        {
            get;
        }

        /// <summary>Whether the tool is currently enabled and therefore offered to the chat model.</summary>
        public bool Enabled
        {
            get;
        }

        /// <summary>Pairs the given tool with its enabled state.</summary>
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

        /// <summary>Describes this tool to the model in the protocol-neutral <see cref="LlmToolDefinition"/> shape.</summary>
        public LlmToolDefinition CreateToolDefinition()
        {
            return Tool.CreateToolDefinition();
        }
    }

    /// <summary>
    /// The outcome of <see cref="McpServerProxyCollection.SetupConfigurationAsync"/>: the tool
    /// container as updated and the list of servers that came up successfully.
    /// </summary>
    public sealed class McpServersSetupConfigurationResult
    {
        /// <summary>The tool container after the requested servers' tools were merged into it.</summary>
        public AvailableToolContainer ToolContainer
        {
            get;
        }
        /// <summary>The servers that were requested and started (or refreshed) successfully.</summary>
        public List<IMcpServerProxy> SuccessStartedMcpServers
        {
            get;
        }

        /// <summary>Pairs the updated tool container with the list of servers that started successfully.</summary>
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
