using OpenAI.Chat;
using System.Collections.Generic;

namespace FreeAIr.Chat.Content
{
    /// <summary>
    /// One element of the dialogue: a user prompt, an answer of the model, or a tool call.
    /// The chat keeps these in chronological order and turns them into request messages via
    /// <see cref="CreateChatMessages"/>.
    ///
    /// One content may produce several messages — a tool call, for instance, becomes both the
    /// assistant message announcing it and the tool message carrying its result.
    /// </summary>
    public interface IChatContent
    {
        /// <summary>Which of prompt, answer or tool call this entry is.</summary>
        ChatContentTypeEnum Type
        {
            get;
        }

        /// <summary>
        /// This chat content may be archived.
        /// If so, this means it is not a subject to send into LLM.
        /// </summary>
        public bool IsArchived
        {
            get;
        }

        /// <summary>
        /// Marks this content as archived. Archived contents stay visible in the UI but are
        /// excluded from the requests, which is how a long dialogue is trimmed without losing
        /// its history.
        /// </summary>
        void Archive();

        /// <summary>
        /// Renders this content as the message(s) to be sent to the model.
        /// </summary>
        IReadOnlyList<ChatMessage> CreateChatMessages();
    }
}
