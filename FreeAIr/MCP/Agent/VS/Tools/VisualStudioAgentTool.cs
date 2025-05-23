using FreeAIr.MCP.Agent.Github.BLO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.VS.Tools
{
    public abstract class VisualStudioAgentTool : AgentTool
    {
        public VisualStudioAgentTool(
            string agentName,
            string toolName,
            string description,
            string parameters
            ) : base(agentName, toolName, description, parameters)
        {
        }

        public abstract Task<AgentToolCallResult> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            );
    }

}
