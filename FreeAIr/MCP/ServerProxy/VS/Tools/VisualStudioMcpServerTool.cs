using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy.VS.Tools
{
    public abstract class VisualStudioMcpServerTool : McpServerTool
    {
        public VisualStudioMcpServerTool(
            string mcpServerProxyName,
            string toolName,
            string description,
            string parameters
            ) : base(mcpServerProxyName, toolName, description, parameters)
        {
        }

        public abstract Task<McpServerProxyToolCallResult> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            );
    }

}
