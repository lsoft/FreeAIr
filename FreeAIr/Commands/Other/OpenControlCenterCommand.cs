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
                ControlCenterSectionEnum.None
                );
        }

        public static async Task ShowAsync(
            ControlCenterSectionEnum section
            )
        {
            var w = new ControlCenterWindow(
                section
                );
            var vm = new ControlCenterViewModel();
            w.DataContext = vm;
            _ = await w.ShowDialogAsync();
        }
    }

}
