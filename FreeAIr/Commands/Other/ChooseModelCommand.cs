using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands.Other
{
    /// <summary>
    /// The menu command that opens the Choose Model tool window, letting the user pick which AI
    /// model/agent is active.
    /// </summary>
    [Command(PackageIds.ChooseModelCommandId)]
    internal sealed class ChooseModelCommand : BaseCommand<ChooseModelCommand>
    {
        /// <summary>
        /// Shows the Choose Model tool window when the user invokes the command.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            _ = await ChooseModelToolWindow.ShowAsync();
        }

        /// <summary>
        /// Completes command initialization; defers entirely to the base implementation.
        /// </summary>
        protected override Task InitializeCompletedAsync()
        {
            return base.InitializeCompletedAsync();
        }

        /// <summary>
        /// Keeps the command always enabled in the menu.
        /// </summary>
        protected override void BeforeQueryStatus(EventArgs e)
        {
            this.Command.Enabled = true;
        }
    }
}
