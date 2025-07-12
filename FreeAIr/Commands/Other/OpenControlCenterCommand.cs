using FreeAIr.Options2;
using FreeAIr.UI.ViewModels;
using FreeAIr.UI.Windows;
using System.Windows;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.OpenControlCenterCommandId)]
    internal sealed class OpenControlCenterCommand : BaseCommand<OpenControlCenterCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await ShowAsync(
                );
        }

        public static async Task ShowAsync(
            )
        {
            var w = new ControlCenterWindow(
                );
            var vm = new ControlCenterViewModel(
                );
            w.DataContext = vm;
            _ = await w.ShowDialogAsync();
        }
    }

}
