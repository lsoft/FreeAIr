using FreeAIr.UI.ViewModels;
using FreeAIr.UI.Windows;
using System.Windows;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.ShowOptionsCommandId)]
    internal sealed class ShowOptionsCommand : BaseCommand<ShowOptionsCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            FreeAIrPackage.Instance.ShowOptionPage(typeof(OptionsProvider.MCPPageOptions));
        }

    }

}
