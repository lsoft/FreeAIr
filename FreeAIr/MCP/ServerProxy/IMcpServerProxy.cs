using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.McpServerProxy
{
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
