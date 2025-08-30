using System.Threading.Tasks;
using FreeAIr.Chat.Context;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public sealed class StringAnswerPart : IParsedPart
    {
        public string Text
        {
            get;
        }

        public StringAnswerPart(string text)
        {
            Text = text;
        }

        public Task<string> AsPromptStringAsync()
        {
            return Task.FromResult(Text);
        }

        public IChatContextItem TryCreateChatContextItem()
        {
            return null;
        }
    }
}
