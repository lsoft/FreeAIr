using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Provides methods to display autocomplete suggestions in the Visual Studio text editor.
    /// </summary>
    public static class SuggestionHijackHelper
    {
        private static readonly MethodInfo _tryDisplaySuggestionAsyncMethod;
        private static readonly MethodInfo _cacheProposalMethod;

        private static readonly FieldInfo _suggestionManagerField;
        private static readonly FieldInfo _sessionField;

        private static readonly Type _generateResultType;
        private static readonly Type _inlineCompletionsType;
        private static readonly Type _inlineCompletionSuggestion;

        static SuggestionHijackHelper()
        {
            var assembly = Assembly.Load("Microsoft.VisualStudio.IntelliCode");

            foreach (var type in assembly.GetTypes())
            {
                if (type.Name == "GenerateResult")
                {
                    _generateResultType = type;
                }
                if (type.Name == "InlineCompletionsInstance")
                {
                    _inlineCompletionsType = type;
                }
                if (type.Name == "InlineCompletionSuggestion")
                {
                    _inlineCompletionSuggestion = type;
                }
            }

            _cacheProposalMethod = _inlineCompletionsType.GetMethod("CacheProposal", BindingFlags.Instance | BindingFlags.NonPublic);
            
            _suggestionManagerField = _inlineCompletionsType.GetField("_suggestionManager", BindingFlags.Instance | BindingFlags.NonPublic);
            _sessionField = _inlineCompletionsType.GetField("Session", BindingFlags.Instance | BindingFlags.NonPublic);

            if (_suggestionManagerField == null)
            {
                _suggestionManagerField = _inlineCompletionsType.GetField("SuggestionManager", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (_suggestionManagerField != null)
            {
                _tryDisplaySuggestionAsyncMethod = _suggestionManagerField.FieldType.GetMethod("TryDisplaySuggestionAsync");
            }
        }

        public static async Task ShowAutocompleteAsync(
            ITextView textView,
            ProposalCollectionBase proposalCollection
            )
        {
            if (textView == null)
            {
                return;
            }
            if (proposalCollection == null)
            {
                return;
            }

            var inlineCompletionsInstance = textView.Properties.PropertyList.FirstOrDefault(x => x.Key is Type && (x.Key as Type).Name == "InlineCompletionsInstance").Value;

            var sessionInstance = _sessionField.GetValue(inlineCompletionsInstance) as SuggestionSessionBase;
            if (sessionInstance != null)
            {
                await sessionInstance.DismissAsync(ReasonForDismiss.DismissedDueToInvalidProposal, new CancellationToken());
            }

            var generateResultInstance = Activator.CreateInstance(_generateResultType, new object[] { proposalCollection, null });
            try
            {
                var ctor = _inlineCompletionSuggestion.GetConstructors(
                    BindingFlags.Instance | BindingFlags.NonPublic
                    ).First();
                var suggestions = ctor.Invoke(
                    new object[]
                    {
                        inlineCompletionsInstance
                    });

                var suggestionManagerInstance = _suggestionManagerField.GetValue(inlineCompletionsInstance);
                var newSession = await (Task<SuggestionSessionBase>)_tryDisplaySuggestionAsyncMethod.Invoke(
                    suggestionManagerInstance,
                    new object[]
                    {
                        suggestions,
                        null
                    }
                    );
                if (newSession is SuggestionSessionBase suggestionSessionBase)
                {
                    _cacheProposalMethod.Invoke(inlineCompletionsInstance, new object[] { proposalCollection.Proposals.First() });
                    _sessionField.SetValue(inlineCompletionsInstance, newSession);
                    await suggestionSessionBase.DisplayProposalAsync(proposalCollection.Proposals.First(), new CancellationToken());
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Provides methods to create and manage proposal collections for text suggestions.
        /// </summary>
        public static class ProposalFactory
        {
            /// <summary>
            /// Creates an empty ProposalCollection.
            /// </summary>
            /// <returns>
            /// An empty ProposalCollection.
            /// </returns>
            public static ProposalCollection CreateEmptyCollection()
            {
                return new ProposalCollection("FreeAIr Proposal Collection", []);
            }

            /// <summary>
            /// Creates a ProposalCollection from the provided text and position within the given ITextView.
            /// </summary>
            /// <param name="gen">The generated text to be included in the proposal.</param>
            /// <param name="textView">The ITextView instance where the proposal will be applied.</param>
            /// <param name="position">The position within the textView where the proposal starts.</param>
            /// <returns>
            /// A ProposalCollection containing the generated proposal.
            /// </returns>
            public static ProposalCollection CreateCollectionFromText(string gen, ITextView textView, int position)
            {
                VirtualSnapshotPoint val = new(textView.TextSnapshot, position);

                SnapshotSpan val2 = new(val.Position, 0);

                ProposedEdit item = new(val2, gen);

                List<ProposedEdit> list = [item];

                List<Proposal> list2 =
                [
                    new Proposal($"FreeAIr Proposal", ImmutableArray.ToImmutableArray(list), val, null, (ProposalFlags)17, null, null, null, null, null)
                ];

                return new ProposalCollection("FreeAIr Proposal Collection", (IReadOnlyList<ProposalBase>)list2);
            }
        }
    }
}
