using FreeAIr.Helper;
using OpenAI.Chat;
using System.Text;

namespace FreeAIr.BLogic.Content
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

        ChatMessage CreateChatMessage();
    }

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

    public sealed class AnswerChatContent : IChatContent
    {
        private readonly StringBuilder _answerBody = new();

        public ChatContentTypeEnum Type => ChatContentTypeEnum.LLMAnswer;

        
        //public event Action AnswerChangedEvent;
        public TimeoutEventProxy<AnswerChangedEventArgs> AnswerChangedEvent;

        public bool IsArchived
        {
            get;
            private set;
        }

        public string AnswerBody => _answerBody.ToString();

        public AnswerChatContent()
        {
            AnswerChangedEvent = new TimeoutEventProxy<AnswerChangedEventArgs>(
                250,
                this,
                (pa0, pa1) =>
                {
                    if (pa0 is null && pa1 is null)
                    {
                        return ArgsActionKindEnum.ReplaceLastArgs;
                    }
                    if (pa0 is null)
                    {
                        return ArgsActionKindEnum.AddToQueue;
                    }
                    if (pa1 is null)
                    {
                        return ArgsActionKindEnum.ReplaceLastArgs;
                    }

                    return ArgsActionKindEnum.ReplaceLastArgs;
                }
                );
        }

        public void Archive()
        {
            IsArchived = true;
        }

        public void Append(string answer)
        {
            _answerBody.Append(answer);

            AnswerChangedEvent.Fire(AnswerChangedEventArgs.Instance);
        }

        public ChatMessage CreateChatMessage()
        {
            return new AssistantChatMessage(AnswerBody);
        }
    }

    public sealed class AnswerChangedEventArgs : EventArgs
    {
        public static readonly AnswerChangedEventArgs Instance = new();
    }
}
