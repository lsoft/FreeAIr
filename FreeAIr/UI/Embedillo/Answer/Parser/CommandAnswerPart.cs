using FreeAIr.BLogic;
using FreeAIr.BLogic.Context;
using FreeAIr.Helper;

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

        public string AsPromptString()
        {
            return Kind.AsPromptString();
        }

        public IChatContextItem TryCreateChatContextItem()
        {
            return null;
        }
    }
}
