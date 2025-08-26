using FreeAIr.Helper;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FreeAIr.Options2.Agent
{
    public sealed class AgentJson : ICloneable
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

        public AgentJson()
        {
            Name = string.Empty;
            Technical = new();
            SystemPrompt = AgentCollectionJson.DefaultSystemPrompt;
        }

        public object Clone()
        {
            return new AgentJson
            {
                Name = Name,
                Technical = (AgentTechnical)Technical.Clone(),
                SystemPrompt = SystemPrompt
            };
        }

        public async Task<string> GetFormattedSystemPromptAsync()
        {
            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();
            var result = SystemPrompt.Replace("{CULTURE}", unsorted.GetAnswerCultureName());
            return result;
        }

        public async Task<bool> VerifyAgentAndShowErrorIfNotAsync()
        {
            return await Technical.VerifyAgentAndShowErrorIfNotAsync();
        }
    }

    [JsonConverter(typeof(JsonDescriptionCommentConverter<AgentTechnical>))]
    public sealed class AgentTechnical : ICloneable
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
        [Description("A secret token for this agent. You can store here your token directly, or you can store your token in MY_VAR environment variable, and use here {$MY_VAR} value to retrieve your token from env vars. Do not forget to restart VS after setting the env var.")]
        public string Token
        {
            get;
            set;
        }

        public string GetToken()
        {
            return DirectOrEnvStringHelper.GetValue(Token);
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
            ChosenModel = string.Empty;
            ContextSize = 8192;
        }

        public object Clone()
        {
            return new AgentTechnical
            {
                Endpoint = Endpoint,
                Token = Token,
                ChosenModel = ChosenModel,
                ContextSize = ContextSize
            };
        }

        public bool HasToken() => !string.IsNullOrEmpty(Token);

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
            return UriHelper.TryBuildEndpointUri(Endpoint);
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
