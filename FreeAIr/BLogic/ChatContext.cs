using FreeAIr.Helper;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FreeAIr.BLogic
{
    public interface IChatContext
    {
        IReadOnlyList<IChatContextItem> Items
        {
            get;
        }

        void AddItem(IChatContextItem item);

        void RemoveItem(IChatContextItem item);
    }

    public sealed class ChatContext : IChatContext
    {
        private readonly List<IChatContextItem> _items = new();

        public event ChatContextChangedDelegate ChatContextChangedEvent;

        public IReadOnlyList<IChatContextItem> Items => _items;

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

    public sealed class ChatContextEventArgs : EventArgs
    {
        public ChatContext Context
        {
            get;
        }

        public ChatContextEventArgs(
            ChatContext context
            )
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Context = context;
        }

    }


    public sealed class DiskFileChatContextItem : IChatContextItem
    {
        public string FilePath
        {
            get;
        }

        public string ContextUIDescription => FilePath;

        public DiskFileChatContextItem(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = Path.GetFullPath(filePath);
        }

        public bool IsSame(IChatContextItem other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is not DiskFileChatContextItem otherDisk)
            {
                return false;
            }

            var result = StringComparer.CurrentCultureIgnoreCase.Compare(
                FilePath,
                otherDisk.FilePath
                ) == 0;

            return result;
        }

        public async Task OpenInNewWindowAsync()
        {
            await VS.Documents.OpenAsync(FilePath);
        }


        public string AsContextPromptText()
        {
            if (!File.Exists(FilePath))
            {
                return $"`File {FilePath} does not found`";
            }

            var fi = new FileInfo(FilePath);

            return
                Environment.NewLine
                + $"Source code of the file `{FilePath}`:"
                + Environment.NewLine
                + Environment.NewLine
                + "```"
                + LanguageHelper.GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(fi.Extension)
                + Environment.NewLine
                + System.IO.File.ReadAllText(FilePath)
                + Environment.NewLine
                + "```"
                + Environment.NewLine;
        }

        public void ReplaceWithText(string body)
        {
            if (body is null)
            {
                throw new ArgumentNullException(nameof(body));
            }

            var lineEnding = LineEndingHelper.Actual.OpenDocumentAndGetLineEnding(
                FilePath
                );


            File.WriteAllText(
                FilePath,
                body.WithLineEnding(lineEnding)
                );
        }
    }

    public interface IChatContextItem
    {
        /// <summary>
        /// Как должен выглядеть этот итем в UI Visual Studio
        /// </summary>
        string ContextUIDescription
        {
            get;
        }

        bool IsSame(IChatContextItem other);

        Task OpenInNewWindowAsync();

        /// <summary>
        /// Заменить текст итема.
        /// </summary>
        /// <param name="body"></param>
        void ReplaceWithText(string body);

        /// <summary>
        /// Как должен выглядеть этот итем в разделе "контекст" промпта.
        /// Например, файловый итем в контексте промпта должен выглядеть так:
        /// 
        /// Файл {{тут полный путь до файла}}:
        /// 
        /// ```csharp {{или другой префикс, зависит от расширения файла}}
        /// {{тут тело файла}}
        /// ```
        /// </summary>
        string AsContextPromptText();

    }

}
