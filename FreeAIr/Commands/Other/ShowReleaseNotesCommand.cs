using FreeAIr.Helper;

namespace FreeAIr.Commands.Other
{
    [Command(PackageIds.ShowReleaseNotesCommandId)]
    internal sealed class ShowReleaseNotesCommand : BaseCommand<ShowReleaseNotesCommand>
    {
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _ = await VS.Documents.OpenAsync(
                "RELEASE_NOTES.md".GetFullPathToFile()
                );
        }

    }

}
