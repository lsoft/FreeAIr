using System.IO;
using System.Threading.Tasks;
using FreeAIr.Chat;
using FreeAIr.Chat.Context;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    /// <summary>
    /// Parsed answer part for an @-mentioned solution item (a file, optionally with a
    /// selected span) in an Embedillo chat input; resolves it into the chat context
    /// item that supplies the file's content to the LLM.
    /// </summary>
    public sealed class SolutionItemAnswerPart : IParsedPart
    {
        /// <summary>
        /// The file (and optional selection) this part refers to.
        /// </summary>
        public SelectedIdentifier SelectedIdentifier
        {
            get;
        }

        /// <summary>
        /// Parses the mention text into a <see cref="SelectedIdentifier"/>.
        /// </summary>
        public SolutionItemAnswerPart(
            string solutionItemText
            )
        {
            if (solutionItemText is null)
            {
                throw new ArgumentNullException(nameof(solutionItemText));
            }

            SelectedIdentifier = SelectedIdentifier.Parse(solutionItemText);
        }

        /// <summary>
        /// Renders the mention back to its compact "path:start-end" text form for
        /// inclusion in the prompt.
        /// </summary>
        public Task<string> AsPromptStringAsync()
        {
            return Task.FromResult(SelectedIdentifier.ToString());
        }

        /// <summary>
        /// Checks whether the referenced file still exists on disk.
        /// </summary>
        public bool IsFileExists()
        {
            return File.Exists(SelectedIdentifier.FilePath);
        }

        /// <summary>
        /// Creates the chat context item that supplies the referenced file's content
        /// to the LLM, or null when the file no longer exists.
        /// </summary>
        public IChatContextItem? TryCreateChatContextItem()
        {
            if (!IsFileExists())
            {
                return null;
            }

            return
                new SolutionItemChatContextItem(
                    SelectedIdentifier,
                    false,
                    AddLineNumbersMode.NotRequired
                    );
        }

    }
}
