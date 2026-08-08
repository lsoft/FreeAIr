using FreeAIr.Helper;
using FreeAIr.UI.ViewModels;
using FreeAIr.UI.Wizard;
using System.Threading.Tasks;
using System.Windows;

namespace FreeAIr.Commands.Other
{
    /// <summary>
    /// The menu command that opens the first-run setup wizard, which replaces FreeAIr's agents,
    /// MCP servers, actions and other settings with ones the user builds themselves.
    /// </summary>
    [Command(PackageIds.OpenSetupWizardCommandId)]
    internal sealed class OpenSetupWizardCommand : BaseCommand<OpenSetupWizardCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await ShowAsync(isFirstRun: false);
        }

        /// <summary>
        /// Builds the wizard window and its view model and shows it as a modal dialog. Anything
        /// that goes wrong while doing so - a corrupted settings file, a theme resource that will
        /// not load - is logged and shown as an error rather than allowed to escape: this runs on
        /// the UI thread inside devenv, where an unhandled exception ends the Visual Studio process
        /// rather than just the wizard.
        /// </summary>
        public static async Task ShowAsync(
            bool isFirstRun
            )
        {
            try
            {
                var vm = new SetupWizardViewModel(isFirstRun);
                await vm.InitializeAsync();

                var w = new SetupWizardWindow();
                w.DataContext = vm;

                _ = await w.ShowDialogAsync();
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();

                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    excp.Message
                    );
            }
        }
    }
}
