using EnvDTE;
using FreeAIr.UI;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.OpenBuildingNaturalLanguageOutlinesWindowCommandId)]
    public sealed class OpenBuildingNaturalLanguageOutlinesWindowCommand : BaseCommand<OpenBuildingNaturalLanguageOutlinesWindowCommand>
    {
        public OpenBuildingNaturalLanguageOutlinesWindowCommand(
            )
        {
        }

        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await BuildNaturalLanguageOutlinesJsonFileToolWindow.ShowPaneAsync(
                true
                );
        }
    }
}
