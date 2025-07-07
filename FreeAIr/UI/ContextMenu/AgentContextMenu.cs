using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.UI.ContextMenu
{

    public sealed class AgentContextMenu
    {
        public static async Task<AgentJson?> ChooseAgentWithTokenAsync(
            string title,
            string? preferredAgentName = null
            )
        {
            if (title is null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            var agentCollection = await FreeAIrOptions.DeserializeAgentCollectionAsync();
            var agents = agentCollection.Agents;
            var filteredAgents = agents.FindAll(a => !string.IsNullOrEmpty(a.Technical.Token));
            if (filteredAgents.Count == 0)
            {
                return null;
            }
            if (filteredAgents.Count == 1)
            {
                return filteredAgents[0];
            }

            if (!string.IsNullOrEmpty(preferredAgentName))
            {
                var preferredAgent = filteredAgents.FirstOrDefault(a => a.Name == preferredAgentName);
                if (preferredAgent is not null)
                {
                    return preferredAgent;
                }
            }

            var chosenAgent = await VisualStudioContextMenuCommandBridge.ShowAsync<AgentJson>(
                title,
                filteredAgents
                    .ConvertAll(a => (a.Name, a as object))
                );

            return chosenAgent;
        }

    }
}
