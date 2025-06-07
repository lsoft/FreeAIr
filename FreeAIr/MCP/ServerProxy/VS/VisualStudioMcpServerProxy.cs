using FreeAIr.MCP.McpServerProxy.VS.Tools;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS
{
    public sealed class VisualStudioMcpServerProxy : IMcpServerProxy
    {
        public const string VisualStudioProxyName = "VisualStudio";

        public static readonly VisualStudioMcpServerProxy Instance = new();

        public string Name => VisualStudioProxyName;

        private readonly Dictionary<string, VisualStudioMcpServerTool> _tools;

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

        public Task<bool> IsInstalledAsync()
        {
            return Task.FromResult(true);
        }

        public Task InstallAsync()
        {
            //nothing to do
            return Task.CompletedTask;
        }

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
