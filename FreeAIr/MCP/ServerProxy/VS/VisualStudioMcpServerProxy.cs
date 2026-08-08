using FreeAIr.MCP.McpServerProxy.VS.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS
{
    /// <summary>
    /// The built-in MCP server proxy that exposes Visual Studio automation as tools (nuget install,
    /// file content search, web search, git commit and similar) without going through an external
    /// MCP process. Tools are discovered by reflection over <see cref="VisualStudioMcpServerTool"/>
    /// subclasses in this assembly.
    /// </summary>
    public sealed class VisualStudioMcpServerProxy : IMcpServerProxy
    {
        /// <summary>
        /// The proxy name shown to the user and used to identify this server among the configured
        /// MCP servers.
        /// </summary>
        public const string VisualStudioProxyName = "VisualStudio";

        /// <summary>
        /// The single shared instance of the Visual Studio proxy; tool discovery happens once, in
        /// the constructor, so the proxy is used as a singleton rather than instantiated per call.
        /// </summary>
        public static readonly VisualStudioMcpServerProxy Instance = new();

        /// <summary>
        /// The display name of this proxy, equal to <see cref="VisualStudioProxyName"/>.
        /// </summary>
        public string Name => VisualStudioProxyName;

        /// <summary>
        /// The tools exposed by this proxy, keyed by tool name, populated once via reflection at
        /// construction time.
        /// </summary>
        private readonly Dictionary<string, VisualStudioMcpServerTool> _tools;

        /// <summary>
        /// Builds the proxy by scanning the assembly for every <see cref="VisualStudioMcpServerTool"/>
        /// subclass and registering its singleton <c>Instance</c> under its tool name.
        /// </summary>
        public VisualStudioMcpServerProxy()
        {
            _tools = new();

            #region add available tools

            var tools =
                from type in typeof(VisualStudioMcpServerProxy).Assembly.GetTypes()
                where type.BaseType == typeof(VisualStudioMcpServerTool)
                let instanceField = type.GetField("Instance", BindingFlags.Static | BindingFlags.Public)
                let tool = instanceField.GetValue(null) as VisualStudioMcpServerTool
                select tool;

            foreach (var tool in tools)
            {
                _tools[tool.ToolName] = tool;
            }

            #endregion
        }

        /// <summary>
        /// Always reports installed, since this proxy runs in-process and has no external server
        /// or package to set up.
        /// </summary>
        public Task<bool> IsInstalledAsync()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// No-op installer, kept only to satisfy <see cref="IMcpServerProxy"/> since this proxy has
        /// nothing to install.
        /// </summary>
        public Task InstallAsync()
        {
            //nothing to do
            return Task.CompletedTask;
        }

        /// <summary>
        /// Looks up the requested tool by name and invokes it with the supplied arguments, returning
        /// <see langword="null"/> when no tool with that name is registered.
        /// </summary>
        public async Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            Dictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            if (!_tools.TryGetValue(toolName, out VisualStudioMcpServerTool tool))
            {
                return null;
            }

            var result = await tool.CallToolAsync(
                toolName,
                arguments,
                cancellationToken
                );
            return result;
        }

        /// <summary>
        /// Returns the full set of tools this proxy exposes, for advertising to the chat model.
        /// </summary>
        public Task<McpServerTools> GetToolsAsync()
        {
            return Task.FromResult(
                new McpServerTools(
                    _tools.Values.ToArray()
                )
                );
        }
    }
}
