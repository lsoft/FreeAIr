using System.Text.Json.Serialization;

namespace Dto
{
    /// <summary>
    /// Base of every request the VS side sends across the MCP proxy pipe: which server it targets
    /// plus a free-form parameter pool (case-insensitive by key) that a concrete request indexes
    /// through <see cref="IParameterProvider"/>.
    /// </summary>
    public abstract class BaseRequest : IParameterProvider
    {
        /// <summary>The free-form parameter values keyed by lower-cased name, serialized alongside the request across the pipe.</summary>
        [JsonInclude]
        public Dictionary<string, string> Pool
        {
            get;
            set;
        } = new();

        /// <summary>Looks up a parameter by name (case-insensitive) in <see cref="Pool"/>; throws if it is missing.</summary>
        public string this[string key]
        {
            get
            {
                var r = Pool[key.ToLower()];
                if (r is null)
                {
                    throw new InvalidOperationException($"Parameter {key} has NULL value");
                }

                return r;
            }
            protected set
            {
                Pool[key.ToLower()] = value;
            }
        }

        /// <summary>Name of the MCP server this request targets.</summary>
        public string MCPServerName
        {
            get;
            set;
        }

        /// <summary>Attempts to read a parameter by name (case-insensitive) from <see cref="Pool"/> without throwing.</summary>
        public bool TryGetValue(string key, out string? value)
        {
            return Pool.TryGetValue(key.ToLower(), out value);
        }

        /// <summary>Parameterless constructor for JSON deserialization.</summary>
        protected BaseRequest()
        {
        }

        /// <summary>Creates a request targeting <paramref name="mcpServerName"/>, seeding <see cref="Pool"/> from <paramref name="parameters"/>.</summary>
        protected BaseRequest(
            string mcpServerName,
            IReadOnlyDictionary<string, string>? parameters
            )
        {
            if (mcpServerName is null)
            {
                throw new ArgumentNullException(nameof(mcpServerName));
            }

            MCPServerName = mcpServerName;

            if (parameters is not null)
            {
                foreach (var pair in parameters)
                {
                    this[pair.Key] = pair.Value;
                }
            }

        }
    }
}
