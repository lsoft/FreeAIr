using FreeAIr.BLogic.Context;
using System.Threading.Tasks;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public sealed class CommandAnswerPart : IParsedPart
    {
        public string Prompt
        {
            get;
        }

        public CommandAnswerPart(
            string prompt
            )
        {
            Prompt = prompt;
        }

        public Task<string> AsPromptStringAsync()
        {
            return Task.FromResult(Prompt);
        }

        public IChatContextItem TryCreateChatContextItem()
        {
            return null;
        }
    }
}
