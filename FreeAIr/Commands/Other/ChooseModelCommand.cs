using FreeAIr.Options2;
using FreeAIr.UI.ToolWindows;
using Xceed.Wpf.Toolkit;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.ChooseModelCommandId)]
    internal sealed class ChooseModelCommand : BaseCommand<ChooseModelCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            _ = await ChooseModelToolWindow.ShowAsync();
        }

        protected override Task InitializeCompletedAsync()
        {
            return base.InitializeCompletedAsync();
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            this.Command.Enabled = true;
        }
    }
}
