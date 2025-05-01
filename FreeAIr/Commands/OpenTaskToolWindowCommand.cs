using FreeAIr.UI.ToolWindows;

namespace FreeAIr.Commands
{
    [Command(PackageIds.FreeAIrOpenTaskToolWindowCommandId)]
    internal sealed class OpenTaskToolWindowCommand : BaseCommand<OpenTaskToolWindowCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            _ = await TaskListToolWindow.ShowAsync();
        }

    }

}
