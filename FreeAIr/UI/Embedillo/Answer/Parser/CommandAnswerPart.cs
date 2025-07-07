using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.Helper;
using System.Threading.Tasks;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public sealed class CommandAnswerPart : IParsedPart
    {
        public ChatKindEnum Kind
        {
            get;
        }

        public CommandAnswerPart(
            ChatKindEnum kind
            )
        {
            Kind = kind;
        }

        public async Task<string> AsPromptStringAsync()
        {
            return await Kind.AsPromptStringAsync();
        }

        public IChatContextItem TryCreateChatContextItem()
        {
            return null;
        }
    }
}
