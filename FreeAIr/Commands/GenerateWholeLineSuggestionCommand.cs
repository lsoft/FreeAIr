using EnvDTE;
using FreeAIr.BLogic;
using FreeAIr.Helper;
using FreeAIr.Options2;

namespace FreeAIr.Commands
{
    [Command(PackageIds.GenerateWholeLineSuggestionCommand)]
    internal sealed class GenerateWholeLineSuggestionCommand : BaseCommand<GenerateWholeLineSuggestionCommand>
    {
        public GenerateWholeLineSuggestionCommand(
            )
        {
        }

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

            var proposalCollection = await proposalSource.CreateProposalSourceAsync(
                caretPosition
                );

            await SuggestionHijackHelper.ShowAutocompleteAsync(
                textView,
                proposalCollection
                );
        }

    }
}
