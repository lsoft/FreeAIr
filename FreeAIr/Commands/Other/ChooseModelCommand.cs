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
            var uri = ApiPage.Instance.TryBuildEndpointUri();
            if (uri is null)
            {
                this.Command.Enabled = false;
                return;
            }

            if (string.Compare(uri.Host, "openrouter.ai", true) == 0)
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
