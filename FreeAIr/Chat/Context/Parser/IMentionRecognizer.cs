namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    /// <summary>
    /// Everything the ANTLR markdown parsers need from a mention kind: the anchor character that
    /// introduces it in the text and the way to turn the text after that anchor into a parsed part.
    /// The editor's AvalonEdit generator implements this, which is what keeps the parsers free of
    /// any dependency on the chat input control and its rendering.
    /// </summary>
    public interface IMentionRecognizer
    {
        /// <summary>
        /// The character that introduces a mention recognized by this recognizer, such as '#' for a
        /// solution item or '/' for a support action.
        /// </summary>
        char AnchorSymbol
        {
            get;
        }

        /// <summary>
        /// Parses the text following the anchor symbol into a typed mention part, or returns null
        /// when it does not denote anything this recognizer knows - the parser then keeps the word
        /// as plain text.
        /// </summary>
        IParsedPart? CreatePart(string partPayload);
    }
}
