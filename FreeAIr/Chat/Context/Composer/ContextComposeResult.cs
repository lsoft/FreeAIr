using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.Chat.Context.Composer
{
    /// <summary>
    /// What <see cref="CSharpContextComposer"/> collected while working out which files should go
    /// into a chat alongside the one the user picked.
    ///
    /// It accumulates from two directions at once — the files the user named, and the files reached
    /// by following type references — and both are sets, because the same file is reached over and
    /// over while walking a graph of references.
    /// </summary>
    public sealed class ContextComposeResult
    {
        private readonly HashSet<ContextSelectedIdentifier> _foundIdentifiers = new();

        //Roslyn symbols must be compared through SymbolEqualityComparer: the same type reached from
        //two compilations is two unequal instances otherwise, and the walk would never terminate
        private readonly HashSet<ITypeSymbol> _types = new(SymbolEqualityComparer.Default);

        /// <summary>Every file gathered so far, each remembering whether a human asked for it.</summary>
        public IReadOnlyCollection<ContextSelectedIdentifier> FoundIdentifiers => _foundIdentifiers;

        /// <summary>
        /// The types already visited, which is how the reference walk knows where it has been and
        /// stops instead of going round a cycle.
        /// </summary>
        public IReadOnlyCollection<ITypeSymbol> Types => _types;

        public ContextComposeResult(
            )
        {
        }

        /// <summary>
        /// Records a file the user chose. Marked as not auto-found, which is what keeps it out of
        /// the "we added these for you" part of the chat window.
        /// </summary>
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
                _foundIdentifiers.Add(new(SelectedIdentifier.Create(filePath, null), isAutoFound));
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

        /// <summary>
        /// Turns the gathered files into context items ready to be attached to a chat.
        ///
        /// Line numbers are switched off for all of them: these files are background the model
        /// should read, not the fragment it is being asked to rewrite, and numbering them all would
        /// cost tokens on every request for nothing.
        /// </summary>
        public IReadOnlyList<SolutionItemChatContextItem> ConvertToChatContextItem()
        {
            return FoundIdentifiers
                .Select(i => new SolutionItemChatContextItem(
                    i.SelectedIdentifier,
                    i.IsAutoFound,
                    AddLineNumbersMode.NotRequired
                    )
                )
                .ToList();
        }
    }
}
