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
        [JsonInclude]
        public Dictionary<string, string> Pool
        {
            get;
            set;
        } = new();

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

        public string MCPServerName
        {
            get;
            set;
        }

        public bool TryGetValue(string key, out string? value)
        {
            return Pool.TryGetValue(key.ToLower(), out value);
        }

        protected BaseRequest()
        {
        }

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
