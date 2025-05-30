using Microsoft.CodeAnalysis;

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
