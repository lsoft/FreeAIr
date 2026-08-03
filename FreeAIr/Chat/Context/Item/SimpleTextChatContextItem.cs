using OpenAI.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreeAIr.Chat.Context.Item
{
    /// <summary>
    /// A piece of context that is just text, with no file behind it.
    ///
    /// This is what carries everything generated rather than read: the git diff a commit message is
    /// written from, build errors, the output of a tool. The text is held in memory, so unlike
    /// <see cref="SolutionItemChatContextItem"/> it is a snapshot — whatever it was when the item
    /// was made is what the model sees.
    /// </summary>
    public sealed class SimpleTextChatContextItem : IChatContextItem
    {
        private string _body;

        /// <summary>
        /// The label shown on the context chip in the chat window. The body may be tens of
        /// kilobytes of diff, so something has to name it in one line.
        /// </summary>
        public string ContextUIDescription
        {
            get;
        }

        public bool IsAutoFound
        {
            get;
        }

        public SimpleTextChatContextItem(
            string contextUIDescription,
            string body,
            bool isAutoFound
            )
        {
            if (contextUIDescription is null)
            {
                throw new ArgumentNullException(nameof(contextUIDescription));
            }

            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            ContextUIDescription = contextUIDescription;
            _body = body;
            IsAutoFound = isAutoFound;
        }

        public Task<string> AsContextPromptTextAsync()
        {
            return Task.FromResult(_body);
        }

        public async Task<UserChatMessage> CreateChatMessageAsync()
        {
            return new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(
                    _body
                    )
                );
        }

        /// <summary>
        /// Two text items are the same when their bodies are. There is no path to compare, and the
        /// description is only a label — two chips reading "Git diff" may well hold different
        /// diffs.
        /// </summary>
        public bool IsSame(IChatContextItem other)
        {
            if (other is not SimpleTextChatContextItem other2)
            {
                return false;
            }

            return StringComparer.CurrentCultureIgnoreCase.Compare(_body, other2._body) == 0;
        }

        /// <summary>
        /// Does nothing: there is no document to open. Clicking such a chip in the chat window is
        /// simply inert.
        /// </summary>
        public Task OpenInNewWindowAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Replaces the text in memory. Nothing is written anywhere — this item is not backed by a
        /// file, so an answer applied to it only changes what the next prompt will carry.
        /// </summary>
        public void ReplaceWithText(string body)
        {
            _body = body;
        }

        /// <summary>
        /// Always empty. Related files are found by walking C# references, and generated text has
        /// no references to walk.
        /// </summary>
        public async Task<IReadOnlyList<SolutionItemChatContextItem>> SearchRelatedContextItemsAsync()
        {
            return [];
        }
    }
}
