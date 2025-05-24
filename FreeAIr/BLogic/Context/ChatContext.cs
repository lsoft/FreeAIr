using FreeAIr.BLogic.Context.Item;
using Microsoft.VisualStudio.TeamFoundation.Git.Extensibility;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;

namespace FreeAIr.BLogic.Context
{
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
                    new CustomFileContextItem(
                        fullPath
                        )
                    );
            }

            return context;
        }

        private static async System.Threading.Tasks.Task<string?> GetFullPathToCopilotInstructionAsync()
        {
            var gitExt = (IGitExt)await FreeAIrPackage.Instance.GetServiceAsync(typeof(IGitExt));
            if (gitExt is not null)
            {
                if (gitExt.ActiveRepositories.Count == 1)
                {
                    var activeRepository = gitExt.ActiveRepositories[0] as IGitRepositoryInfo2;
                    if (activeRepository.Remotes.Count == 1)
                    {
                        var repositoryFolder = activeRepository.RepositoryPath;

                        var repoFullPath = Path.Combine(repositoryFolder, CopilotInstructionFilePath);
                        if (File.Exists(repoFullPath))
                        {
                            return repoFullPath;
                        }
                    }
                }
            }

            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            var solutionFolder = new FileInfo(solution.FullPath).Directory.FullName;
            var solutionFullPath = Path.Combine(solutionFolder, CopilotInstructionFilePath);
            if (File.Exists(solutionFullPath))
            {
                return solutionFullPath;
            }

            return null;
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
