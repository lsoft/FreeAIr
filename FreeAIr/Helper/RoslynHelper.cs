using FreeAIr.Shared.Helper;
using Microsoft.CodeAnalysis;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Roslyn syntax and symbol tree walking helpers used to locate an enclosing declaration
    /// (e.g. the type or member a piece of code belongs to) when building the natural language
    /// outline or resolving context for a suggestion.
    /// </summary>
    public static class RoslynHelper
    {
        /// <summary>
        /// Walks up the syntax tree from <paramref name="node"/> and returns the nearest ancestor
        /// (or the node itself) whose type is one of <paramref name="parentTypes"/>, or <c>null</c>
        /// if none is found before the root.
        /// </summary>
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

        /// <summary>
        /// Walks up the symbol's containing-symbol chain and returns the outermost ancestor that
        /// is assignable to <typeparamref name="T"/>, e.g. finding the top-level containing type
        /// of a nested member symbol.
        /// </summary>
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
