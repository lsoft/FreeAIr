using OpenAI.Chat;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FreeAIr.BLogic;

namespace FreeAIr.Chat.Content
{
    /// <summary>
    /// One answer of the model inside a chat, growing token by token while the completion streams.
    ///
    /// The text arrives in dozens of chunks a second, and every chunk has to reach the chat window
    /// so the user watches the answer being written. Raising an event per chunk would repaint the
    /// markdown faster than it can be rendered, so the notification goes through a
    /// <see cref="TimeoutEventProxy{T}"/> which coalesces the flood into one update per quarter of
    /// a second. The text itself is never delayed — only the notification is.
    /// </summary>
    public sealed class AnswerChatContent : IChatContent, IAsyncDisposable
    {
        /// <summary>Accumulates the streamed answer chunks into the full answer text.</summary>
        private readonly StringBuilder _answerBody = new();

        /// <summary>Always <see cref="ChatContentTypeEnum.LLMAnswer"/>.</summary>
        public ChatContentTypeEnum Type => ChatContentTypeEnum.LLMAnswer;

        /// <summary>
        /// Raised, at most a few times a second, after the answer has grown. Carries no payload:
        /// the subscriber rereads <see cref="AnswerBody"/> in full.
        /// </summary>
        public TimeoutEventProxy<AnswerChangedEventArgs> AnswerChangedEvent;

        /// <summary>
        /// An archived answer stays in the chat window but is no longer sent back to the model as
        /// history, which is how a chat is kept from growing past the context window.
        /// </summary>
        public bool IsArchived
        {
            get;
            private set;
        }

        /// <summary>Everything the model has said so far, as one string.</summary>
        public string AnswerBody => _answerBody.ToString();

        /// <summary>
        /// Creates an empty, growing answer and sets up the coalesced change notification.
        /// </summary>
        public AnswerChatContent()
        {
            //every pending notification says the same thing — "the answer has changed" — so a queued
            //one is replaced rather than added to; the subscriber reads the whole body anyway
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

        /// <summary>Drops this answer out of the history sent to the model. Cannot be undone.</summary>
        public void Archive()
        {
            IsArchived = true;
        }

        /// <summary>
        /// Adds the next streamed chunk to the answer and lets the chat window know. Called once per
        /// chunk from the reader, so it has to stay cheap.
        /// </summary>
        public async Task AppendAsync(string answer)
        {
            _answerBody.Append(answer);

            await AnswerChangedEvent.FireAsync(AnswerChangedEventArgs.Instance);
        }

        /// <summary>
        /// This answer as history for the next request: a single assistant message holding the whole
        /// body.
        /// </summary>
        public IReadOnlyList<ChatMessage> CreateChatMessages()
        {
            return
                [
                    new AssistantChatMessage(AnswerBody)
                ];
        }

        /// <summary>Disposes the underlying <see cref="AnswerChangedEvent"/> proxy.</summary>
        public async ValueTask DisposeAsync()
        {
            await AnswerChangedEvent.DisposeAsync();
        }
    }

    /// <summary>
    /// The payload of <see cref="AnswerChatContent.AnswerChangedEvent"/>, which is nothing at all.
    /// One shared instance, because thousands of these are raised while an answer streams and none
    /// of them carries anything to distinguish it.
    /// </summary>
    public sealed class AnswerChangedEventArgs : EventArgs
    {
        /// <summary>The single shared, payload-less instance raised for every answer change.</summary>
        public static readonly AnswerChangedEventArgs Instance = new();
    }

}
