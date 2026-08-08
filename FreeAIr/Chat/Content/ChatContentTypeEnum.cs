namespace FreeAIr.Chat.Content
{
    /// <summary>
    /// Which kind of entry a piece of chat content is. The chat window picks the template to render
    /// it with from this, and the history builder decides from it which role the message goes out
    /// under.
    /// </summary>
    public enum ChatContentTypeEnum
    {
        /// <summary>Something the user sent, context items included.</summary>
        Prompt,

        /// <summary>Text the model produced.</summary>
        LLMAnswer,

        /// <summary>A tool the model asked to run, together with whatever the tool answered.</summary>
        ToolCall
    }
}
