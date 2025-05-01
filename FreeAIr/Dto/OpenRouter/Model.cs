using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FreeAIr.Dto.OpenRouter
{
    public class ModelResponse
    {
        [JsonPropertyName("data")]
        public Model[] Models
        {
            get; set;
        }
    }

    [DebuggerDisplay("{name}")]
    public class Model
    {
        public string id
        {
            get; set;
        }
        public string name
        {
            get; set;
        }
        public int created
        {
            get; set;
        }
        public string description
        {
            get; set;
        }
        public int context_length
        {
            get; set;
        }
        public Architecture architecture
        {
            get; set;
        }
        public Pricing pricing
        {
            get; set;
        }
        public Top_Provider top_provider
        {
            get; set;
        }
        public object per_request_limits
        {
            get; set;
        }
        public string[] supported_parameters
        {
            get; set;
        }
    }

    public class Architecture
    {
        public string modality
        {
            get; set;
        }
        public string[] input_modalities
        {
            get; set;
        }
        public string[] output_modalities
        {
            get; set;
        }
        public string tokenizer
        {
            get; set;
        }
        public string instruct_type
        {
            get; set;
        }
    }

    public class Pricing
    {
        public string prompt
        {
            get; set;
        }
        public string completion
        {
            get; set;
        }
        public string request
        {
            get; set;
        }
        public string image
        {
            get; set;
        }
        public string web_search
        {
            get; set;
        }
        public string internal_reasoning
        {
            get; set;
        }
        public string input_cache_read
        {
            get; set;
        }
        public string input_cache_write
        {
            get; set;
        }

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

        private bool IsEmpty(string q)
        {
            return q is null || q == "0";
        }
    }

    public class Top_Provider
    {
        public int? context_length
        {
            get; set;
        }
        public int? max_completion_tokens
        {
            get; set;
        }
        public bool is_moderated
        {
            get; set;
        }
    }
}
