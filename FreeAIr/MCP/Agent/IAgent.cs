using FreeAIr.MCP.Agent.Github.BLO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent
{
    public interface IAgent
    {
        string Name
        {
            get;
        }

        Task<bool> IsInstalledAsync();

        Task InstallAsync();
        
        Task<AgentTools> GetToolsAsync();
        
        Task<AgentToolCallResult?> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            );
    }
}
