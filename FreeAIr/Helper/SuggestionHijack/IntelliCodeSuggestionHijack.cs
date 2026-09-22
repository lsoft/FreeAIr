using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Language.Suggestions;
using Microsoft.VisualStudio.Text.Editor;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Helper.SuggestionHijack
{
    /// <summary>
    /// The hijack for Visual Studio 2022, where the inline completion machinery lives in
    /// <c>Microsoft.VisualStudio.IntelliCode</c> as plain internal fields. This is the path that has
    /// shipped since the feature was written and it is kept exactly as it was: everything about the
    /// IntelliCode internals changed in Visual Studio 2026, so trying to serve both from one piece
    /// of reflection would only put the working IDE at risk. See
    /// <see cref="ProbingSuggestionHijack"/> for the newer one.
    /// </summary>
    internal sealed class IntelliCodeSuggestionHijack : ISuggestionHijack
    {
        /// <summary>Reflected IntelliCode method that displays a suggestion session for a completion.</summary>
        private readonly MethodInfo _tryDisplaySuggestionAsyncMethod;
        /// <summary>Reflected IntelliCode method that caches the accepted proposal on the completions instance.</summary>
        private readonly MethodInfo _cacheProposalMethod;

        /// <summary>Reflected field holding IntelliCode's suggestion manager instance.</summary>
        private readonly FieldInfo _suggestionManagerField;
        /// <summary>Reflected field holding the current IntelliCode suggestion session.</summary>
        private readonly FieldInfo _sessionField;

        /// <summary>IntelliCode's internal <c>GenerateResult</c> type, located via reflection.</summary>
        private readonly Type _generateResultType;
        /// <summary>IntelliCode's internal <c>InlineCompletionsInstance</c> type, located via reflection.</summary>
        private readonly Type _inlineCompletionsType;
        /// <summary>IntelliCode's internal <c>InlineCompletionSuggestion</c> type, located via reflection.</summary>
        private readonly Type _inlineCompletionSuggestion;

        /// <summary>
        /// Builds the hijack, or returns <c>null</c> and writes to the Activity Log when the
        /// IntelliCode assembly does not hold what this path expects - which is what happens on any
        /// IDE that is not Visual Studio 2022.
        /// </summary>
        internal static IntelliCodeSuggestionHijack TryCreate()
        {
            try
            {
                return new IntelliCodeSuggestionHijack();
            }
            catch (Exception excp)
            {
                excp.ActivityLogException(
                    "FreeAIr cannot reach the IntelliCode inline completion internals of Visual Studio 2022; whole line suggestions will not be shown."
                    );
                return null;
            }
        }

        /// <summary>
        /// Locates the private IntelliCode types and members needed to hijack its inline
        /// suggestion UI, via reflection over the <c>Microsoft.VisualStudio.IntelliCode</c> assembly.
        /// </summary>
        private IntelliCodeSuggestionHijack()
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

        /// <summary>
        /// Displays a FreeAIr-generated completion in the editor by piggy-backing on IntelliCode's
        /// inline suggestion UI (ghost text), dismissing any existing session first. This is how
        /// FreeAIr shows AI-generated code completions without shipping its own suggestion adorner.
        /// </summary>
        public async Task ShowAutocompleteAsync(
            ITextView textView,
            ProposalCollectionBase proposalCollection
            )
        {
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
    }
}
