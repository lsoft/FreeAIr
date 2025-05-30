using System.Text.Json.Serialization;

namespace Dto
{
    public abstract class BaseRequest : IParameterProvider
    {
        [JsonInclude]
        private Dictionary<string, string> _pool = new();

        public string this[string key]
        {
            get
            {
                var r = _pool[key.ToLower()];
                if (r is null)
                {
                    throw new InvalidOperationException($"Parameter {key} has NULL value");
                }

                return r;
            }
            protected set
            {
                _pool[key.ToLower()] = value;
            }
        }

        public string MCPServerName
        {
            get;
            set;
        }

        public bool TryGetValue(string key, out string? value)
        {
            return _pool.TryGetValue(key.ToLower(), out value);
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
