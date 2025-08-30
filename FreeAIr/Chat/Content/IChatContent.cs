using OpenAI.Chat;
using System.Collections.Generic;

namespace FreeAIr.Chat.Content
{
    public interface IChatContent
    {
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

        void Archive();

        IReadOnlyList<ChatMessage> CreateChatMessages();
    }
}
