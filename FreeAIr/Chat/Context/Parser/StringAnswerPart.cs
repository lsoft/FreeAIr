using System.Threading.Tasks;
using FreeAIr.Chat.Context;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    /// <summary>
    /// Parsed answer part for a plain run of free text in an Embedillo chat input,
    /// carried through unchanged between the other structured parts (commands, files,
    /// solution items).
    /// </summary>
    public sealed class StringAnswerPart : IParsedPart
    {
        /// <summary>
        /// The literal text of this part.
        /// </summary>
        public string Text
        {
            get;
        }

        /// <summary>
        /// Wraps the given literal text as a plain text part.
        /// </summary>
        public StringAnswerPart(string text)
        {
            Text = text;
        }

        /// <summary>
        /// Returns the literal text unchanged for inclusion in the prompt.
        /// </summary>
        public Task<string> AsPromptStringAsync()
        {
            return Task.FromResult(Text);
        }

        /// <summary>
        /// Plain text parts carry no context data, so no chat context item is produced.
        /// </summary>
        public IChatContextItem TryCreateChatContextItem()
        {
            return null;
        }
    }
}
