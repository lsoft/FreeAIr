using FreeAIr.Helper;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Chat.Content
{
    public sealed class AnswerChatContent : IChatContent, IAsyncDisposable
    {
        private readonly StringBuilder _answerBody = new();

        public ChatContentTypeEnum Type => ChatContentTypeEnum.LLMAnswer;

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

        public async Task AppendAsync(string answer)
        {
            _answerBody.Append(answer);

            await AnswerChangedEvent.FireAsync(AnswerChangedEventArgs.Instance);
        }

        public IReadOnlyList<ChatMessage> CreateChatMessages()
        {
            return
                [
                    new AssistantChatMessage(AnswerBody)
                ];
        }

        public async ValueTask DisposeAsync()
        {
            await AnswerChangedEvent.DisposeAsync();
        }
    }

    public sealed class AnswerChangedEventArgs : EventArgs
    {
        public static readonly AnswerChangedEventArgs Instance = new();
    }

}
