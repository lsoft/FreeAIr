namespace FreeAIr.Llm
{
    /// <summary>
    /// Whether the model may call a tool while answering. Only the two states FreeAIr actually
    /// uses are here: the chat lets the model decide, and every automatic chat - a commit message,
    /// a dictation clean-up, a whole line completion - forbids tools outright.
    /// </summary>
    public enum LlmToolChoice
    {
        /// <summary>The model decides whether to call a tool. What an ordinary chat runs under.</summary>
        Auto,

        /// <summary>
        /// No tool may be called. Also what an empty tool list collapses to, since some providers
        /// reject a request which offers none.
        /// </summary>
        None
    }
}
