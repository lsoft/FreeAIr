using FreeAIr.Helper;
using System.Linq;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
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

            if (chat.Status == ChatStatusEnum.Ready)
            {
                var lastPrompt = chat.Prompts.Last();
                if (!lastPrompt.Answer.IsEmpty)
                {
                    var textAnswer = lastPrompt.Answer.GetTextualAnswer();
                    if (!string.IsNullOrEmpty(textAnswer))
                    {
                        var cleanAnswer = textAnswer.CleanupFromQuotesAndThinks(
                            lineEnding
                            );
                        if (!string.IsNullOrEmpty(cleanAnswer))
                        {
                            return cleanAnswer;
                        }
                    }
                }
            }

            return null;
        }
    }
}
