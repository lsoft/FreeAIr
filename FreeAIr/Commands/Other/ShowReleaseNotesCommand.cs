using FreeAIr.Helper;

namespace FreeAIr.Commands.Other
{
    /// <summary>
    /// The menu command that opens RELEASE_NOTES.md in the editor, showing the extension's
    /// version history to the user.
    /// </summary>
    [Command(PackageIds.ShowReleaseNotesCommandId)]
    internal sealed class ShowReleaseNotesCommand : BaseCommand<ShowReleaseNotesCommand>
    {
        /// <summary>
        /// Opens RELEASE_NOTES.md when the user invokes the command.
        /// </summary>
        protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _ = await VS.Documents.OpenAsync(
                "RELEASE_NOTES.md".GetFullPathToFile()
                );
        }

    }

}
