using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Text.Editor;
using System.Threading.Tasks;

namespace FreeAIr.Helper.SuggestionHijack
{
    /// <summary>
    /// One way of borrowing the editor's inline completion UI - the grey ghost text - to display a
    /// FreeAIr proposal. There is an implementation per generation of Visual Studio because the
    /// internals reflected over are not an API and moved between assemblies in Visual Studio 2026;
    /// <see cref="SuggestionHijackHelper"/> picks which one the running IDE gets.
    /// </summary>
    internal interface ISuggestionHijack
    {
        /// <summary>
        /// Shows the proposals as ghost text in the view, dismissing whatever suggestion session is
        /// already displayed there first. A failure is reported to the Activity Log rather than
        /// thrown: the caller is a menu command and a broken hijack must not take it down.
        /// </summary>
        Task ShowAutocompleteAsync(
            ITextView textView,
            ProposalCollectionBase proposalCollection
            );
    }
}
