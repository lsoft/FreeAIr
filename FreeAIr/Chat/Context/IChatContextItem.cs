using OpenAI.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.Chat.Context
{
    /// <summary>
    /// One piece of the chat context: a solution document, a piece of a document, an external
    /// file, an image, and so on. Implementations live in `FreeAIr.Chat.Context.Item`.
    ///
    /// An item is not a snapshot: its text is read when the request is being built, so edits made
    /// in Visual Studio after the item was added are still visible to the LLM.
    /// </summary>
    public interface IChatContextItem
    {
        /// <summary>
        /// Как должен выглядеть этот итем в UI Visual Studio
        /// </summary>
        string ContextUIDescription
        {
            get;
        }

        /// <summary>
        /// Итем найден автоматически, сканнером.
        /// </summary>
        bool IsAutoFound
        {
            get;
        }

        /// <summary>
        /// Identity comparison used to keep the context free of duplicates. Two items are the same
        /// when they refer to the same thing, even if they are different objects added in
        /// different ways.
        /// </summary>
        bool IsSame(IChatContextItem other);

        /// <summary>
        /// Opens what this item refers to in a Visual Studio window; invoked when the user clicks
        /// the item in the context area.
        /// </summary>
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
        Task<string> AsContextPromptTextAsync();

        /// <summary>
        /// Wraps <see cref="AsContextPromptTextAsync"/> into a message ready to be put into the
        /// request. Called every time a request is built, so the content is always up to date.
        /// </summary>
        Task<UserChatMessage> CreateChatMessageAsync();

        /// <summary>
        /// Returns the items this one depends on — for C# files these are the documents found via
        /// Roslyn references. Used by the "add dependent files" button next to a document.
        /// Returns an empty list for the kinds of items that have no notion of dependencies.
        /// </summary>
        Task<IReadOnlyList<SolutionItemChatContextItem>> SearchRelatedContextItemsAsync();
    }

}
