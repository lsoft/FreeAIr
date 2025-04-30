
namespace FreeAIr.Commands
{
    [Command(PackageIds.FreeAIrChooseModelCommandId)]
    internal sealed class ChooseModelCommand : BaseCommand<ChooseModelCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            
        }

        protected override void BeforeQueryStatus(EventArgs e)
        {
            var uri = new Uri(ApiPage.Instance.Endpoint);
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
