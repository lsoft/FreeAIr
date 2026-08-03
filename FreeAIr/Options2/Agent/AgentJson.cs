using FreeAIr.Helper;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FreeAIr.Options2.Agent
{
    /// <summary>
    /// One configured LLM: where to reach it, how to authenticate, which model to ask for and what
    /// system prompt it works under.
    ///
    /// Agents are how the product supports several models at once without knowing anything about
    /// them. A chat, a support action, the embedding index and whole line completion each name the
    /// agent they want, and a user may keep a local model for cheap work beside a cloud one for the
    /// hard questions.
    /// </summary>
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

        /// <summary>
        /// Builds an unusable agent with the default system prompt, for the deserializer and for the
        /// `add agent` button. It has no endpoint or model until the user fills them in.
        /// </summary>
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

        /// <summary>
        /// The system prompt with the `{CULTURE}` anchor replaced by the answer language from the
        /// settings. This is the whole mechanism behind answers in the user's own language: the
        /// prompt names a culture at the last moment, not at configuration time.
        /// </summary>
        public async Task<string> GetFormattedSystemPromptAsync()
        {
            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();
            var result = SystemPrompt.Replace("{CULTURE}", unsorted.GetAnswerCultureName());
            return result;
        }

        /// <summary>
        /// Checks the agent is usable and complains to the user if it is not. Called before a chat
        /// is started, so a misconfigured agent is reported once and clearly instead of surfacing
        /// later as an HTTP error out of the streaming reader.
        /// </summary>
        public async Task<bool> VerifyAgentAndShowErrorIfNotAsync()
        {
            return await Technical.VerifyAgentAndShowErrorIfNotAsync();
        }
    }

    /// <summary>
    /// How to reach an agent: endpoint, token, model and context size.
    ///
    /// Everything here is OpenAI compatible, which is the only requirement the product places on a
    /// provider — Yandex, OpenRouter, a local KoboldCpp and anything else speaking that protocol are
    /// configured identically.
    /// </summary>
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

        /// <summary>
        /// The token, resolved through <see cref="DirectOrEnvStringHelper"/>: a value of the form
        /// `{$MY_VAR}` is read from that environment variable instead. That is what lets a settings
        /// file naming real agents be committed to the repository.
        /// </summary>
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

        /// <summary>
        /// Defaults aimed at a local model: KoboldCpp on its usual port, no token, 8192 tokens of
        /// context. A new agent is therefore usable without any cloud account at all.
        /// </summary>
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

        /// <summary>
        /// Whether a token was configured at all. Used by the agent pickers to hide agents which
        /// cannot be used — note that a local server legitimately needs none, which is why
        /// <see cref="FreeAIrOptions.DeserializeAgentByNameAsync"/> does not apply this filter.
        /// </summary>
        public bool HasToken() => !string.IsNullOrEmpty(Token);

        /// <summary>
        /// Reports the two configuration mistakes worth catching before a request goes out: an
        /// endpoint which is not a valid uri, and a missing token. Both are shown as a message box
        /// naming what to fix.
        /// </summary>
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

        /// <summary>The endpoint as a uri, or null when it does not parse — typically a missing protocol prefix.</summary>
        public Uri? TryBuildEndpointUri()
        {
            return UriHelper.TryBuildEndpointUri(Endpoint);
        }

        /// <summary>
        /// Whether this agent goes through openrouter.ai, which needs handling of its own: the model
        /// list is fetched differently and the provider adds fields to the responses.
        /// </summary>
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
