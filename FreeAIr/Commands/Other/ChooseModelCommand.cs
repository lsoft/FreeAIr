using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.ChooseModelCommandId)]
    internal sealed class ChooseModelCommand : BaseCommand<ChooseModelCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            _ = await ChooseModelToolWindow.ShowAsync();
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            var agent = InternalPage.Instance.GetActiveAgent();
            var uri = agent.TryBuildEndpointUri();
            if (agent.IsOpenRouterAgent())
            {
                this.Command.Enabled = true;
            }
            else
            {
                this.Command.Enabled = false;
            }
        }
    }
}
