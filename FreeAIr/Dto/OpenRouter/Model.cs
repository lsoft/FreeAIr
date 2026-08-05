using System.Diagnostics;
using System.Text.Json.Serialization;

namespace FreeAIr.Dto.OpenRouter
{
    /// <summary>
    /// The JSON envelope returned by the OpenRouter models list endpoint, wrapping the array of
    /// available models under the "data" key.
    /// </summary>
    public class ModelResponse
    {
        /// <summary>
        /// The models OpenRouter currently offers, as reported by the "data" field of the models
        /// list response.
        /// </summary>
        [JsonPropertyName("data")]
        public Model[] Models
        {
            get; set;
        }
    }

    /// <summary>
    /// One entry from the OpenRouter model catalog: an id, its pricing and context window, and the
    /// modalities it supports. Used to populate the model picker for OpenRouter-backed agents.
    /// </summary>
    [DebuggerDisplay("{name}")]
    public class Model
    {
        /// <summary>
        /// The OpenRouter model identifier, e.g. "openai/gpt-4o", used when configuring an agent to
        /// call this model.
        /// </summary>
        public string id
        {
            get; set;
        }
        /// <summary>
        /// The human readable model name shown in the OpenRouter model picker.
        /// </summary>
        public string name
        {
            get; set;
        }
        /// <summary>
        /// The Unix timestamp of when this model was added to OpenRouter's catalog.
        /// </summary>
        public int created
        {
            get; set;
        }
        /// <summary>
        /// The vendor-provided description of the model, shown to help a user choose between models
        /// in the picker.
        /// </summary>
        public string description
        {
            get; set;
        }
        /// <summary>
        /// The maximum number of tokens the model accepts in a single request, used to warn a user
        /// when their context would overflow.
        /// </summary>
        public int context_length
        {
            get; set;
        }
        /// <summary>
        /// The input/output modalities and tokenizer this model supports.
        /// </summary>
        public Architecture architecture
        {
            get; set;
        }
        /// <summary>
        /// The per-token and per-request pricing for this model, used to tell free models apart from
        /// paid ones.
        /// </summary>
        public Pricing pricing
        {
            get; set;
        }
        /// <summary>
        /// The provider-specific limits OpenRouter reports for whichever backend currently serves
        /// this model.
        /// </summary>
        public Top_Provider top_provider
        {
            get; set;
        }
        /// <summary>
        /// Provider-specific per-request limits, passed through from OpenRouter as an opaque object.
        /// </summary>
        public object per_request_limits
        {
            get; set;
        }
        /// <summary>
        /// The request parameters (e.g. "tools", "temperature") this model accepts, used to decide
        /// which features of a chat request are safe to send.
        /// </summary>
        public string[] supported_parameters
        {
            get; set;
        }
    }

    /// <summary>
    /// Describes what kinds of input and output a model can handle, e.g. text-only versus text and
    /// image, and which tokenizer and instruction format it expects.
    /// </summary>
    public class Architecture
    {
        /// <summary>
        /// The overall modality of the model, such as "text-&gt;text" or "text+image-&gt;text".
        /// </summary>
        public string modality
        {
            get; set;
        }
        /// <summary>
        /// The kinds of input the model accepts, e.g. "text", "image".
        /// </summary>
        public string[] input_modalities
        {
            get; set;
        }
        /// <summary>
        /// The kinds of output the model produces, e.g. "text".
        /// </summary>
        public string[] output_modalities
        {
            get; set;
        }
        /// <summary>
        /// The tokenizer family the model uses, relevant for estimating token counts.
        /// </summary>
        public string tokenizer
        {
            get; set;
        }
        /// <summary>
        /// The instruction/prompt format the model expects, when it differs from the provider default.
        /// </summary>
        public string instruct_type
        {
            get; set;
        }
    }

    /// <summary>
    /// The per-token and per-request costs OpenRouter reports for a model, expressed as strings of
    /// decimal price per unit. Used to compute <see cref="IsFree"/> and to display cost in the model
    /// picker.
    /// </summary>
    public class Pricing
    {
        /// <summary>
        /// The price per prompt token.
        /// </summary>
        public string prompt
        {
            get; set;
        }
        /// <summary>
        /// The price per completion token.
        /// </summary>
        public string completion
        {
            get; set;
        }
        /// <summary>
        /// The flat price per request, independent of token counts.
        /// </summary>
        public string request
        {
            get; set;
        }
        /// <summary>
        /// The price per image input.
        /// </summary>
        public string image
        {
            get; set;
        }
        /// <summary>
        /// The price per web search performed by the model.
        /// </summary>
        public string web_search
        {
            get; set;
        }
        /// <summary>
        /// The price per token of internal reasoning, for models that bill reasoning tokens
        /// separately from completion tokens.
        /// </summary>
        public string internal_reasoning
        {
            get; set;
        }
        /// <summary>
        /// The price per token read from the prompt cache.
        /// </summary>
        public string input_cache_read
        {
            get; set;
        }
        /// <summary>
        /// The price per token written to the prompt cache.
        /// </summary>
        public string input_cache_write
        {
            get; set;
        }

        /// <summary>
        /// True when every price field is missing or zero, meaning OpenRouter serves this model at
        /// no cost. Used to flag free models in the picker.
        /// </summary>
        public bool IsFree =>
            IsEmpty(prompt)
            && IsEmpty(completion)
            && IsEmpty(request)
            && IsEmpty(image)
            && IsEmpty(web_search)
            && IsEmpty(internal_reasoning)
            && IsEmpty(input_cache_read)
            && IsEmpty(input_cache_write)
            ;

        /// <summary>
        /// Treats a null price or a literal "0" as no charge for that pricing dimension.
        /// </summary>
        private bool IsEmpty(string q)
        {
            return q is null || q == "0";
        }
    }

    /// <summary>
    /// The limits reported for whichever upstream provider OpenRouter is currently routing a model
    /// to, since the same model id can be served by more than one backend.
    /// </summary>
    public class Top_Provider
    {
        /// <summary>
        /// The context window this specific provider allows, which can be smaller than the model's
        /// overall <see cref="Model.context_length"/>.
        /// </summary>
        public int? context_length
        {
            get; set;
        }
        /// <summary>
        /// The maximum number of completion tokens this provider allows in a response.
        /// </summary>
        public int? max_completion_tokens
        {
            get; set;
        }
        /// <summary>
        /// Whether this provider applies content moderation to requests and responses.
        /// </summary>
        public bool is_moderated
        {
            get; set;
        }
    }
}
