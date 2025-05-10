using FreeAIr.BLogic.Context;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public sealed class StringAnswerPart : IAnswerPart
    {
        public string Text
        {
            get;
        }

        public StringAnswerPart(string text)
        {
            Text = text;
        }

        public string AsPromptString()
        {
            return Text;
        }

        public IChatContextItem TryCreateChatContextItem()
        {
            return null;
        }
    }
}
