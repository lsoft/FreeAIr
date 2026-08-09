namespace FreeAIr.Interaction
{
    /// <summary>
    /// The commit message text box of Visual Studio's Git Changes pane, as far as the commit
    /// message builder is concerned: somewhere to put the text the model produced. The pane is not
    /// FreeAIr's own window and is not always there, hence <see cref="IsAvailable"/>.
    /// </summary>
    public interface IGitCommitMessageBox
    {
        /// <summary>
        /// Whether the Git Changes pane has been found and patched, meaning
        /// <see cref="SetText"/> will reach something the user can see.
        /// </summary>
        bool IsAvailable
        {
            get;
        }

        /// <summary>
        /// Replaces whatever stands in the commit message box with the given text. Must be called
        /// on the UI thread.
        /// </summary>
        void SetText(string text);
    }
}
