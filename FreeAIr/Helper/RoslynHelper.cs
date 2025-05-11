using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class RoslynHelper
    {
        public static T? UpToUpper<T>(
            this ISymbol symbol
            ) where T : ISymbol
        {
            T result = default;

            while (symbol is not null)
            {
                if (symbol is T t)
                {
                    result = t;
                }

                symbol = symbol.ContainingSymbol;
            }

            return result;
        }
    }
}
