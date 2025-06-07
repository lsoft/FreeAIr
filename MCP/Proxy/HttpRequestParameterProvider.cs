using Dto;
using Microsoft.AspNetCore.Http;

namespace Proxy
{
    public sealed class HttpRequestParameterProvider : IParameterProvider
    {
        private readonly IQueryCollection _queryCollection;

        public HttpRequestParameterProvider(
            IQueryCollection queryCollection
            )
        {
            ArgumentNullException.ThrowIfNull(queryCollection);
            _queryCollection = queryCollection;
        }

        public string this[string key]
        {
            get
            {
                var r = _queryCollection[key];
                if (((string)r) is null)
                {
                    throw new InvalidOperationException($"Parameter {key} has NULL value");
                }

                return r;
            }
        }

        public bool TryGetValue(string key, out string? value)
        {
            var r = _queryCollection.TryGetValue(key, out var vvalue);
            if (!r)
            {
                value = null;
                return false;
            }

            value = vvalue;
            return true;
        }
    }
}
