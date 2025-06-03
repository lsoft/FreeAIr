using FreeAIr.BLogic.Context;
using System.Threading.Tasks;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public interface IParsedPart
    {
        /// <summary>
        /// Как должен выглядеть этот парт непосредственно
        /// в теле промпта.
        /// например, файловый парт в теле промпта должен выглядеть просто
        /// как путь до файла (контекст даст LLM полный текст файла).
        /// </summary>
        Task<string> AsPromptStringAsync();

        /// <summary>
        /// Создает итем для контекста чата.
        /// </summary>
        /// <returns>null если этот парт неприменим к созданию итемов контекста чата.</returns>
        IChatContextItem? TryCreateChatContextItem();
    }
}
