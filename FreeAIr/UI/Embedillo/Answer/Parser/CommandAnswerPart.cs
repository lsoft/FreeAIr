using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.Helper;
using System.Threading.Tasks;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public sealed class CommandAnswerPart : IAnswerPart
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

        public Task<string> AsPromptStringAsync()
        {
            return Task.FromResult(
                Kind.AsPromptString()
                );
        }

        public IChatContextItem TryCreateChatContextItem()
        {
            return null;
        }
    }
}
