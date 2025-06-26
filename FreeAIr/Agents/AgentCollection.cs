using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreeAIr.Agents
{
    public sealed class AgentCollection
    {
        public List<Agent> Agents
        {
            get;
            set;
        } = new();

        public Agent GetActiveAgent()
        {
            return TryGetActiveAgent() ?? new Agent();
        }

        public Agent? TryGetActiveAgent()
        {
            if (Agents.Count == 0)
            {
                return null;
            }

            return Agents.FirstOrDefault(a => a.IsDefault) ?? Agents[0];
        }


        public static bool TryParse(
            string optionAgentsJson,
            out AgentCollection? agents
            )
        {
            try
            {
                agents = JsonSerializer.Deserialize<AgentCollection>(optionAgentsJson);
                return true;
            }
            catch
            {
                //error in json
                //todo log
            }

            agents = null;
            return false;
        }

        public async Task<bool> VerifyAgentAndShowErrorIfNotAsync()
        {
            var optionAgent = TryGetActiveAgent();
            if (optionAgent is null)
            {
                return false;
            }

            return await optionAgent.Technical.VerifyAgentAndShowErrorIfNotAsync();
        }

        public Uri? TryBuildEndpointUri()
        {
            var optionAgent = TryGetActiveAgent();
            if (optionAgent is null)
            {
                return null;
            }

            return optionAgent.Technical.TryBuildEndpointUri();
        }

        public void SetDefaultAgent(Agent agent)
        {
            var foundAgent = Agents.FirstOrDefault(a => a.Name == agent.Name); //ReferenceEquals may not work, if Agents are different objects for the same agent itself (from different deserialization)
            if (foundAgent is null)
            {
                return;
            }

            Agents.ForEach(a => a.IsDefault = ReferenceEquals(foundAgent, a));
        }

        public void RemoveWithNoTokenAvailable()
        {
            Agents.RemoveAll(a => !a.Technical.HasToken());
        }

        public void SortByDefaultState()
        {
            Agents.Sort((a, b) => b.IsDefault.CompareTo(a.IsDefault));
        }
    }
}
