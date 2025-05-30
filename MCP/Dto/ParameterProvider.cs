using System;
using System.Collections.Generic;
using System.Text;

namespace Dto
{
    public interface IParameterProvider
    {
        string this[string key]
        {
            get;
        }

        bool TryGetValue(string key, out string? value);
    }
}
