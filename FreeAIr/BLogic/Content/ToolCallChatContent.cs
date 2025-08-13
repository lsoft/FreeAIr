using OpenAI.Chat;

namespace FreeAIr.BLogic.Content
{
    public sealed class ToolCallChatContent : IChatContent
    {
        public ChatContentTypeEnum Type => ChatContentTypeEnum.ToolCall;

        public bool IsArchived
        {
            get;
            private set;
        }

        public void Archive()
        {
            IsArchived = true;
        }

        public ChatMessage CreateChatMessage()
        {
            throw new NotImplementedException();
        }
    }
}
