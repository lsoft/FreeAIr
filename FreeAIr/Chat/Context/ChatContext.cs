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
        public const string CopilotInstructionFilePath = ".github/copilot-instructions.md";

        private readonly List<IChatContextItem> _items = new();

        public event ChatContextChangedDelegate ChatContextChangedEvent;

        public IReadOnlyList<IChatContextItem> Items => _items;

        private ChatContext()
        {
            
        }

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

        private void RaiseChatContextChanged()
        {
            var e = ChatContextChangedEvent;
            if (e is not null)
            {
                e(this, new ChatContextEventArgs(this));
            }
        }

    }

    public delegate void ChatContextChangedDelegate(object sender, ChatContextEventArgs e);
}
