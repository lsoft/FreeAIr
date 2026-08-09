using System.Threading.Tasks;
using FreeAIr.Chat.Context;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    /// <summary>
    /// Parsed part of an Embedillo answer that carries a literal command or free text
    /// prompt fragment, as opposed to a file or solution item reference.
    /// </summary>
    public sealed class CommandAnswerPart : IParsedPart
    {
        /// <summary>
        /// The literal text of this part, inserted verbatim into the prompt.
        /// </summary>
        public string Prompt
        {
            get;
        }

        /// <summary>
        /// Wraps the given literal text as a command part.
        /// </summary>
        public CommandAnswerPart(
            string prompt
            )
        {
            Prompt = prompt;
        }

        /// <summary>
        /// Returns the literal text unchanged, since a command part is already
        /// what should appear in the prompt body.
        /// </summary>
        public Task<string> AsPromptStringAsync()
        {
            return Task.FromResult(Prompt);
        }

        /// <summary>
        /// Command parts carry no context data, so no chat context item is produced.
        /// </summary>
        public IChatContextItem TryCreateChatContextItem()
        {
            return null;
        }
    }
}
