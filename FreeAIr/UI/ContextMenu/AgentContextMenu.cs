using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using System.Linq;
using System.Threading.Tasks;

namespace FreeAIr.UI.ContextMenu
{

    public static class AgentContextMenu
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
            var filteredAgents = agentCollection.FilterAgents();
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

        /// <summary>
        /// The same picker over every agent there is, token or none.
        ///
        /// For embeddings the token says nothing about whether an agent can be used: a local server
        /// is normally configured without one, and it is the only kind of server most users run an
        /// embedding model on. Offering only the agents which do have a token leaves the list full
        /// of cloud chat agents — and when exactly one of those exists, the picker above hands it
        /// over without asking, which is how a query ends up at an endpoint that has no embeddings
        /// at all.
        /// </summary>
        public static async Task<AgentJson?> ChooseAnyAgentAsync(
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
            if (agents.Count == 0)
            {
                return null;
            }
            if (agents.Count == 1)
            {
                //not a choice
                return agents[0];
            }

            if (!string.IsNullOrEmpty(preferredAgentName))
            {
                var preferredAgent = agents.FirstOrDefault(a => a.Name == preferredAgentName);
                if (preferredAgent is not null)
                {
                    return preferredAgent;
                }
            }

            return await VisualStudioContextMenuCommandBridge.ShowAsync<AgentJson>(
                title,
                agents
                    .ConvertAll(a => (a.Name, a as object))
                );
        }
    }
}
