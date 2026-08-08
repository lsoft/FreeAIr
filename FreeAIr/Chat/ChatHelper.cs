using FreeAIr.Helper;
using System.Linq;
using System.Threading.Tasks;
using FreeAIr.Chat.Content;

namespace FreeAIr.Chat
{
    /// <summary>
    /// Extension helpers for driving a <see cref="Chat"/> from code (rather than from the chat
    /// window UI) and reading back a ready-to-use answer.
    /// </summary>
    public static class ChatHelper
    {
        /// <summary>
        /// Sends a prompt and waits for the chat to settle, then returns the last LLM answer with
        /// markdown quoting and reasoning ("think") blocks stripped out. Used by the automatic
        /// chats (commit message, dictation clean-up, ...) that need plain text rather than a chat
        /// transcript. Returns null if the turn failed or produced no usable answer.
        /// </summary>
        public static async Task<string?> WaitForPromptCleanAnswerAsync(
            this Chat chat,
            string lineEnding
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            if (lineEnding is null)
            {
                throw new ArgumentNullException(nameof(lineEnding));
            }

            await chat.WaitForPromptResultAsync();

            if (chat.Status != ChatStatusEnum.Ready)
            {
                return null;
            }

            var lastAnswer = (AnswerChatContent)chat.Contents.LastOrDefault(c => c.Type == Content.ChatContentTypeEnum.LLMAnswer);
            if (lastAnswer is null)
            {
                return null;
            }

            var textAnswer = lastAnswer.AnswerBody;
            if (string.IsNullOrEmpty(textAnswer))
            {
                return null;
            }

            var cleanAnswer = textAnswer.CleanupFromQuotesAndThinks(
                lineEnding
                );
            if (string.IsNullOrEmpty(cleanAnswer))
            {
                return null;
            }

            return cleanAnswer;
        }
    }
}
