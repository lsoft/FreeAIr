using FreeAIr.Agents;
using FreeAIr.UI.ViewModels;
using FreeAIr.UI.Windows;
using System.Windows;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.ShowAgentConfigureCommandId)]
    internal sealed class ShowAgentConfigureCommand : BaseCommand<ShowAgentConfigureCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await ShowAsync(
                InternalPage.Instance.GetAgentCollection()
                );
        }

        public static async Task ShowAsync(
            AgentCollection agentCollection
            )
        {
            var w = new AgentConfigureWindow(
                );
            var vm = new AgentConfigureViewModel(
                agentCollection
                );
            w.DataContext = vm;
            _ = await w.ShowDialogAsync();
        }
    }

}
