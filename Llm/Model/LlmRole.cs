namespace FreeAIr.Llm
{
    /// <summary>
    /// Who said one message of a dialogue. This is the neutral vocabulary the chat is built in;
    /// each transport maps it onto whatever its own protocol calls the same thing, and the two
    /// protocols disagree enough that the mapping is not a rename.
    /// </summary>
    public enum LlmRole
    {
        /// <summary>
        /// The user talking to the model, and also the carrier of the material FreeAIr attaches on
        /// the user's behalf - the context documents and selections.
        /// </summary>
        User,

        /// <summary>
        /// The model talking back. Carries the answer text, the tool calls it asked for, or both.
        /// </summary>
        Assistant,

        /// <summary>
        /// The reply of a tool the model asked to run. OpenAI sends this as a role of its own;
        /// Anthropic has no such role and carries the same thing as a block inside a user message,
        /// which is why this is a role here rather than a message type.
        /// </summary>
        Tool
    }
}
