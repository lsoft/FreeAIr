namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.OpenPropertiesCommandId)]
    internal sealed class OpenPropertiesCommand : BaseCommand<OpenPropertiesCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            FreeAIrPackage.Instance.ShowOptionPage(typeof(OptionsProvider.FontSizePageOptions));
        }

    }

}
