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
            var activeAgent = await FreeAIrOptions.GetActiveAgentAsync();
            if (activeAgent is null)
            {
                await VS.MessageBox.ShowErrorAsync(
                    "Set openrouter agent as active.",
                    Resources.Resources.Error
                    );
                return;
            }

            var uri = activeAgent.Technical.TryBuildEndpointUri();
            if (!activeAgent.Technical.IsOpenRouterAgent())
            {
                await VS.MessageBox.ShowErrorAsync(
                    "Set openrouter agent as active.",
                    Resources.Resources.Error
                    );
                return;
            }

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
