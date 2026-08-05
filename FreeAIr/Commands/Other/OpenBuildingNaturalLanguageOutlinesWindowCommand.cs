using EnvDTE;
using FreeAIr.UI;

namespace FreeAIr.Commands.Other
{
    /// <summary>
    /// The menu command that opens the "Building Natural Language Outlines" tool window, showing
    /// progress of generating the NLO/RAG comments used by the natural language search index.
    /// </summary>
    [Command(PackageIds.OpenBuildingNaturalLanguageOutlinesWindowCommandId)]
    public sealed class OpenBuildingNaturalLanguageOutlinesWindowCommand : BaseCommand<OpenBuildingNaturalLanguageOutlinesWindowCommand>
    {
        /// <summary>
        /// Creates the command instance; no setup is required beyond the base VS command wiring.
        /// </summary>
        public OpenBuildingNaturalLanguageOutlinesWindowCommand(
            )
        {
        }

        /// <summary>
        /// Shows the Building Natural Language Outlines tool window pane.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await BuildNaturalLanguageOutlinesJsonFileToolWindow.ShowPaneAsync(
                true
                );
        }
    }
}
