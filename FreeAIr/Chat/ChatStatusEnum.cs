namespace FreeAIr.Chat
{
    /// <summary>
    /// Where a chat is in its request cycle. Drives what the chat window shows — the spinner, the
    /// Stop button, the error styling — and whether a new prompt may be sent at all.
    /// </summary>
    public enum ChatStatusEnum
    {
        /// <summary>Created, but no prompt has ever been sent.</summary>
        NotStarted,

        /// <summary>The request is out and the first token has not come back yet.</summary>
        WaitingForAnswer,

        /// <summary>The completion is streaming in and the answer grows as it arrives.</summary>
        ReadingAnswer,

        /// <summary>Idle and ready for the next prompt. Also where a cancelled read lands.</summary>
        Ready,

        /// <summary>
        /// The turn ended in an error, which has been written into the chat as the answer. The chat
        /// stays usable — the next prompt clears this.
        /// </summary>
        Failed
    }
}
