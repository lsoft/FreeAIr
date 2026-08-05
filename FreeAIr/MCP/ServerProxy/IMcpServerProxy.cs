using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy
{
    /// <summary>
    /// The VSIX side's view of one MCP server, whatever is behind it - a VS-internal tool set, the
    /// GitHub server, or an external one reached through the out-of-process proxy. Everything above
    /// this (tool discovery, tool calling, UI) works only against this interface.
    /// </summary>
    public interface IMcpServerProxy
    {
        string Name
        {
            get;
        }

        Task<bool> IsInstalledAsync();

        Task InstallAsync();
        
        Task<McpServerTools> GetToolsAsync();
        
        Task<McpServerProxyToolCallResult?> CallToolAsync(
            string toolName,
            Dictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            );
    }
}
