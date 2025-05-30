using Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent
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
