using FreeAIr.BLogic.Context.Item;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreeAIr.BLogic.Context
{
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
        Task<string> AsContextPromptTextAsync();

        Task<UserChatMessage> CreateChatMessageAsync();

        Task<IReadOnlyList<SolutionItemChatContextItem>> SearchRelatedContextItemsAsync();
    }

}
