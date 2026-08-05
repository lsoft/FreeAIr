namespace FreeAIr.Commands.Other
{
    /// <summary>
    /// The menu command that opens Visual Studio's Options dialog to FreeAIr's UI settings page.
    /// </summary>
    [Command(PackageIds.OpenPropertiesCommandId)]
    internal sealed class OpenPropertiesCommand : BaseCommand<OpenPropertiesCommand>
    {
        /// <summary>
        /// Opens the Options dialog on the FreeAIr UI options page when the user invokes the command.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            FreeAIrPackage.Instance.ShowOptionPage(typeof(OptionsProvider.UIPageOptions));
        }

    }

}
