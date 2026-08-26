using FreeAIr.Git;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.Chat.Context
{
    /// <summary>
    /// The material handed to the LLM in addition to the prompts: solution documents, selections,
    /// external files, images and so on.
    ///
    /// Context items are compared by <see cref="IChatContextItem.IsSame"/> rather than by
    /// reference, so adding the same document twice is a no-op no matter how it was added
    /// (typed as `#name`, dragged from Solution Explorer, pulled in as a C# dependency, ...).
    ///
    /// See <see cref="FreeAIr.Chat.Chat.GetMessageListAsync"/> for where exactly the context lands in the
    /// request.
    /// </summary>
    public sealed class ChatContext
    {
        /// <summary>
        /// The repository-relative path of the instruction file shared with GitHub Copilot, looked
        /// for in every new chat.
        /// </summary>
        public const string CopilotInstructionFilePath = ".github/copilot-instructions.md";

        /// <summary>The context items currently attached to the chat, backing <see cref="Items"/>.</summary>
        private readonly List<IChatContextItem> _items = new();

        /// <summary>
        /// Raised whenever items are added or removed, which is what redraws the row of context
        /// chips under the prompt box.
        /// </summary>
        public event ChatContextChangedDelegate ChatContextChangedEvent;

        /// <summary>The context items currently attached to the chat, in the order they were added.</summary>
        public IReadOnlyList<IChatContextItem> Items => _items;

        /// <summary>Creates an empty context; use <see cref="CreateChatContextAsync"/> to also pick up the project's Copilot instructions.</summary>
        private ChatContext()
        {

        }

        /// <summary>
        /// An empty context with nothing attached, used when a chat is rebuilt from disk: the saved
        /// chips are added afterwards, and Copilot instructions must not be re-attached on top of
        /// whatever the file already recorded.
        /// </summary>
        public static ChatContext CreateEmpty()
        {
            return new ChatContext();
        }

        /// <summary>
        /// A context for a new chat, already carrying the project's Copilot instructions when there
        /// are any. Attached as auto-found, so a user who does not want them can clear them along
        /// with the rest of the automatic context.
        /// </summary>
        public static async System.Threading.Tasks.Task<ChatContext> CreateChatContextAsync(
            )
        {
            var context = new ChatContext();

            var fullPath = await GetFullPathToCopilotInstructionAsync();
            if (!string.IsNullOrEmpty(fullPath))
            {
                context._items.Add(
                    new CustomFileChatContextItem(
                        fullPath,
                        true
                        )
                    );
            }

            return context;
        }

        /// <summary>
        /// Looks for `copilot-instructions.md` first in the git repository root and then next to
        /// the solution. If your project already tells Copilot how to behave, FreeAIr obeys the
        /// same instructions instead of asking you to duplicate them.
        /// </summary>
        private static async System.Threading.Tasks.Task<string?> GetFullPathToCopilotInstructionAsync()
        {
            var repositoryFolder = await GitRepositoryProvider.GetRepositoryFolderAsync();
            if (!string.IsNullOrEmpty(repositoryFolder))
            {
                var repoFullPath = Path.GetFullPath(
                    Path.Combine(repositoryFolder, CopilotInstructionFilePath)
                    );
                if (File.Exists(repoFullPath))
                {
                    return repoFullPath;
                }
            }

            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution is null)
            {
                return null;
            }

            var solutionFolder = new FileInfo(solution.FullPath).Directory.FullName;
            var solutionFullPath = Path.GetFullPath(
                Path.Combine(solutionFolder, CopilotInstructionFilePath)
                );
            if (File.Exists(solutionFullPath))
            {
                return solutionFullPath;
            }

            return null;
        }

        /// <summary>
        /// Removes every item matching one of the given ones. Matching is by
        /// <see cref="IChatContextItem.IsSame"/>, so a caller may pass a freshly built item to drop
        /// the equivalent one already held.
        /// </summary>
        public void RemoveItems(
            IReadOnlyList<IChatContextItem> items
            )
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var removedCount = _items.RemoveAll(i => items.Any(ii => ii.IsSame(i)));
            if (removedCount > 0)
            {
                RaiseChatContextChanged();
            }
        }

        /// <summary>
        /// Removes every item equivalent to the given one, per <see cref="IChatContextItem.IsSame"/>.
        /// </summary>
        public void RemoveItem(
            IChatContextItem item
            )
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var removedCount = _items.RemoveAll(i => i.IsSame(item));
            if (removedCount > 0)
            {
                RaiseChatContextChanged();
            }
        }

        /// <summary>
        /// Adds the items that are not there yet, then notifies once for the whole batch — the
        /// reference walker adds dozens at a time, and a redraw per item would be visible.
        /// </summary>
        public void AddItems(
            IReadOnlyList<IChatContextItem> items
            )
        {
            if (items is null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            foreach (var item in items)
            {
                if (_items.Any(i => i.IsSame(item)))
                {
                    continue;
                }

                _items.Add(item);
            }

            RaiseChatContextChanged();
        }

        /// <summary>
        /// Adds one item unless an equivalent one is already attached. Silent about the duplicate:
        /// the same file arriving from the user and from the reference walker is expected, not an
        /// error worth telling anybody about.
        /// </summary>
        public void AddItem(
            IChatContextItem item
            )
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (_items.Any(i => i.IsSame(item)))
            {
                return;
            }

            _items.Add(item);

            RaiseChatContextChanged();
        }

        /// <summary>
        /// Drops the items the scanner found on its own, keeping the ones the user added
        /// deliberately (see <see cref="IChatContextItem.IsAutoFound"/>).
        /// </summary>
        public void RemoveAutomaticItems()
        {
            var removedCount = _items.RemoveAll(i => i.IsAutoFound);
            if (removedCount > 0)
            {
                RaiseChatContextChanged();
            }
        }

        /// <summary>Fires <see cref="ChatContextChangedEvent"/> so the UI redraws the row of context chips.</summary>
        private void RaiseChatContextChanged()
        {
            var e = ChatContextChangedEvent;
            if (e is not null)
            {
                e(this, new ChatContextEventArgs(this));
            }
        }

    }

    /// <summary>Handler shape of <see cref="ChatContext.ChatContextChangedEvent"/>.</summary>
    public delegate void ChatContextChangedDelegate(object sender, ChatContextEventArgs e);
}
