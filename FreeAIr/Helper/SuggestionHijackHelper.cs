using FreeAIr.Helper.SuggestionHijack;
using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Provides methods to display autocomplete suggestions in the Visual Studio text editor.
    ///
    /// The suggestion UI being borrowed is not an API and its internals were rearranged in Visual
    /// Studio 2026, so the reflection lives in an <see cref="ISuggestionHijack"/> per IDE
    /// generation and this class only decides which one the running IDE gets.
    /// </summary>
    public static class SuggestionHijackHelper
    {
        /// <summary>
        /// The way this IDE's inline suggestion UI is borrowed, or <c>null</c> when the editor
        /// internals could not be reached at all and whole line suggestions are unavailable.
        /// </summary>
        private static readonly ISuggestionHijack _hijack;

        /// <summary>
        /// Picks the hijack for the running IDE: Visual Studio 2022 keeps the path which has always
        /// worked there, everything newer goes through the probing one. Nothing here may throw -
        /// this is a type initializer, and a failure in it surfaces as a
        /// <c>TypeInitializationException</c> from an unrelated call site, which is exactly how
        /// issue #75 was reported.
        /// </summary>
        static SuggestionHijackHelper()
        {
            try
            {
                _hijack = IsVisualStudio2022()
                    ? IntelliCodeSuggestionHijack.TryCreate()
                    : (ISuggestionHijack)ProbingSuggestionHijack.TryCreate();
            }
            catch (Exception excp)
            {
                excp.ActivityLogException(
                    "FreeAIr cannot borrow the editor's inline suggestion UI; whole line suggestions will not be shown."
                    );
            }
        }

        /// <summary>
        /// Whether the host is Visual Studio 2022. The major version of <c>devenv.exe</c> is the
        /// cheapest reliable answer and needs neither the UI thread nor a shell service, which a
        /// type initializer running on an arbitrary thread cannot assume. A version which cannot be
        /// read counts as "not 2022", because the probing hijack copes with either IDE.
        /// </summary>
        private static bool IsVisualStudio2022()
        {
            try
            {
                using var process = Process.GetCurrentProcess();
                return process.MainModule?.FileVersionInfo?.FileMajorPart == 17;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException(
                    "FreeAIr cannot read the version of the host process; assuming Visual Studio 2026 or newer."
                    );
                return false;
            }
        }

        /// <summary>
        /// Displays a FreeAIr-generated completion in the editor by piggy-backing on the editor's
        /// inline suggestion UI (ghost text), dismissing any existing session first. This is how
        /// FreeAIr shows AI-generated code completions without shipping its own suggestion adorner.
        /// </summary>
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
            if (_hijack is null)
            {
                ActivityLogHelper.ActivityLogWarning(
                    "FreeAIr cannot show a whole line suggestion: the editor's inline completion internals were not found."
                    );
                return;
            }

            await _hijack.ShowAutocompleteAsync(
                textView,
                proposalCollection
                );
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
