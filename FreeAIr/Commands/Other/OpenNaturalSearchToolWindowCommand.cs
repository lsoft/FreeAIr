using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.OpenNaturalSearchToolWindowCommandId)]
    internal sealed class OpenNaturalSearchToolWindowCommand : BaseCommand<OpenNaturalSearchToolWindowCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _ = await NaturalLanguageResultsToolWindow.ShowAsync();
        }

    }

}
