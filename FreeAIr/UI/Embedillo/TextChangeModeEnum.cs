namespace FreeAIr.UI.Embedillo
{
    /// <summary>Identifies which kind of edit just occurred in the prompt editor, so mention/visual-line handling can react to word-deletes and selection replacement differently from a plain backspace.</summary>
    public enum TextChangeModeEnum
    {
        /// <summary>Ctrl+Backspace: deletes the word to the left of the caret.</summary>
        CtrlBackspace,
        /// <summary>Backspace: deletes a single character to the left of the caret.</summary>
        Backspace,
        /// <summary>Ctrl+Delete: deletes the word to the right of the caret.</summary>
        CtrlDelete,
        /// <summary>Delete: deletes a single character to the right of the caret.</summary>
        Delete,
        /// <summary>Typing or pasting over an active text selection.</summary>
        SelectionReplace
    }
}