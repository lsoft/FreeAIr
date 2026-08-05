using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.Helper;

namespace FreeAIr.Commands
{

    /// <summary>
    /// The editor command that triggers an AI-generated whole-line completion suggestion at the
    /// caret and shows it through the editor's autocomplete/proposal UI.
    /// </summary>
    [Command(PackageIds.GenerateWholeLineSuggestionCommandId)]
    public sealed class GenerateWholeLineSuggestionCommand : BaseCommand<GenerateWholeLineSuggestionCommand>
    {
        /// <summary>
        /// Creates the command instance; no setup is required beyond the base VS command wiring.
        /// </summary>
        public GenerateWholeLineSuggestionCommand(
            )
        {
        }

        /// <summary>
        /// Builds a proposal source for the active text view at the caret position and shows the
        /// generated whole-line suggestion via the autocomplete hijack helper.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var documentView = await VS.Documents.GetActiveDocumentViewAsync();
            if (documentView == null)
            {
                //not a text window
                return;
            }

            var textView = documentView.TextView;
            if (textView == null)
            {
                //not a text window
                return;
            }

            var proposalSource = new ProposalSource(
                textView
                );

            var caretPosition = textView.Caret.Position.BufferPosition;

            //the user asked for a suggestion and is waiting for it, so a misconfigured whole line
            //completion is worth an error dialog here - unlike on the implicit typing path
            var proposalCollection = await proposalSource.CreateProposalSourceAsync(
                caretPosition,
                invokedExplicitly: true
                );

            await SuggestionHijackHelper.ShowAutocompleteAsync(
                textView,
                proposalCollection
                );
        }

    }
}
