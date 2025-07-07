using FreeAIr.Shared.Helper;
using Microsoft.CodeAnalysis;

namespace FreeAIr.Helper
{
    public static class RoslynHelper
    {
        public static SyntaxNode? UpTo(
            this SyntaxNode node,
            params Type[] parentTypes
            )
        {
            while (node is not null)
            {
                if (node.GetType().In(parentTypes))
                {
                    return node;
                }

                node = node.Parent;
            }

            return null;
        }

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
