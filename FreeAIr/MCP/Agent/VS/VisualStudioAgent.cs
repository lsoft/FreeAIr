using FreeAIr.MCP.Agent.Github.BLO;
using FreeAIr.MCP.Agent.VS.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.VS
{
    public sealed class VisualStudioAgent : IAgent
    {
        public const string VisualStudioAgentName = "VisualStudio";

        public static readonly VisualStudioAgent Instance = new();

        public string Name => VisualStudioAgentName;

        private readonly Dictionary<string, VisualStudioAgentTool> _tools;

        public VisualStudioAgent()
        {
            _tools = new();

            #region add available tools

            _tools[GetAllSolutionItemsTool.VisualStudioToolName] = GetAllSolutionItemsTool.Instance;
            _tools[GetSolutionItemBodyTool.VisualStudioToolName] = GetSolutionItemBodyTool.Instance;

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

        public async Task<AgentToolCallResult?> CallToolAsync(
            string toolName,
            IReadOnlyDictionary<string, object?>? arguments = null,
            CancellationToken cancellationToken = default
            )
        {
            if (!_tools.TryGetValue(toolName, out VisualStudioAgentTool tool))
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

        public Task<AgentTools> GetToolsAsync()
        {
            return Task.FromResult(
                new AgentTools(
                    _tools.Values.ToArray()
                )
                );
        }
    }
}
