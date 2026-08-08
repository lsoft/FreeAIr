using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands.Other
{
    /// <summary>
    /// The menu command that opens the Natural Language Search results tool window, used to run
    /// and inspect NLO/RAG queries against the embedding index.
    /// </summary>
    [Command(PackageIds.OpenNaturalSearchToolWindowCommandId)]
    internal sealed class OpenNaturalSearchToolWindowCommand : BaseCommand<OpenNaturalSearchToolWindowCommand>
    {
        /// <summary>
        /// Shows the Natural Language Search results tool window when the user invokes the command.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _ = await NaturalLanguageResultsToolWindow.ShowAsync();
        }

    }

}
