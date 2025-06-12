using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Agents
{
    public sealed class Agent
    {
        /// <summary>
        /// Agent name.
        /// </summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// This agent is the default choice for any new chat.
        /// </summary>
        public bool IsDefault
        {
            get;
            set;
        }

        /// <summary>
        /// Technical info about this agent.
        /// </summary>
        public AgentTechnical Technical
        {
            get;
            set;
        }

        /// <summary>
        /// Agent's system prompt.
        /// </summary>
        public string SystemPrompt
        {
            get;
            set;
        }

        public Agent()
        {
            Name = string.Empty;
            IsDefault = false;
            Technical = new();
            SystemPrompt = InternalPage.DefaultSystemPrompt;
        }

        public string GetFormattedSystemPrompt()
        {
            var result = SystemPrompt.Replace("{CULTURE}", ResponsePage.GetAnswerCultureName());
            return result;
        }
    }

    public sealed class AgentTechnical
    {
        /// <summary>
        /// An endpoint of LLM API provider.
        /// </summary>
        public string Endpoint
        {
            get;
            set;
        }

        /// <summary>
        /// A token of LLM API provider.
        /// </summary>
        public string Token
        {
            get;
            set;
        }

        /// <summary>
        /// Chosen model, if API provider suggests many
        /// </summary>
        public string ChosenModel
        {
            get;
            set;
        }

        /// <summary>
        /// A LLM context size. It depends on model.
        /// </summary>
        public int ContextSize
        {
            get;
            set;
        }

        public AgentTechnical()
        {
            Endpoint = "http://localhost:5001/v1";
            Token = string.Empty;
            ContextSize = 8192;
        }

        public async Task<bool> VerifyAgentAndShowErrorIfNotAsync()
        {
            var endpointUri = TryBuildEndpointUri();
            if (endpointUri is null)
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    "Invalid endpoint Uri. Please make sure it is correct uri. For example, the uri must contain a protocol prefix, like http, https."
                    );
                return false;
            }
            if (string.IsNullOrEmpty(Token))
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    "Empty access token for chosen agent. Set the actual token via FreeAIr control center and repeat."
                    );
                return false;
            }

            return true;
        }

        public Uri? TryBuildEndpointUri()
        {
            try
            {
                return new Uri(Endpoint);
            }
            catch (Exception excp)
            {
                //todo log
            }

            return null;
        }

        public bool IsOpenRouterAgent()
        {
            var uri = TryBuildEndpointUri();
            if (uri is null)
            {
                return false;
            }

            if (string.Compare(uri.Host, "openrouter.ai", true) == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
