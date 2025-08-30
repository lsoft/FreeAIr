using FreeAIr.Helper;
using System.Linq;
using System.Threading.Tasks;
using FreeAIr.Chat.Content;

namespace FreeAIr.Chat
{
    public static class ChatHelper
    {
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
