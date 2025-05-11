using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace FreeAIr.BLogic.Context.Composer
{
    public sealed class ContextComposeResult
    {
        private readonly HashSet<ContextSelectedIdentifier> _foundIdentifiers = new();
        private readonly HashSet<ITypeSymbol> _types = new(SymbolEqualityComparer.Default);

        public IReadOnlyCollection<ContextSelectedIdentifier> FoundIdentifiers => _foundIdentifiers;

        public IReadOnlyCollection<ITypeSymbol> Types => _types;

        public ContextComposeResult(
            )
        {
        }

        public void AddUserProvidedIdentifier(
            SelectedIdentifier identifier
            )
        {
            if (identifier is null)
            {
                throw new ArgumentNullException(nameof(identifier));
            }

            _foundIdentifiers.Add(
                new ContextSelectedIdentifier(
                    identifier,
                    false
                    )
                );
        }

        public void AddFilePaths(
            IEnumerable<string> filePaths,
            bool isAutoFound
            )
        {
            if (filePaths is null)
            {
                throw new ArgumentNullException(nameof(filePaths));
            }

            foreach (var filePath in filePaths)
            {
                _foundIdentifiers.Add(new( new(filePath, null), isAutoFound));
            }
        }

        public void AddTypes(
            IEnumerable<ITypeSymbol> types
            )
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            foreach (var type in types)
            {
                _types.Add(type);
            }
        }
    }
}
