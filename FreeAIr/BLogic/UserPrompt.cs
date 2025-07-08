using FreeAIr.Helper;
using OpenAI.Chat;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// Контракт для пользовательских запросов к ИИ-ассистенту
    /// </summary>
    public sealed class UserPrompt
    {
        /// <inheritdoc />
        public string PromptBody
        {
            get;
        }

        /// <inheritdoc />
        public LLMAnswer Answer
        {
            get;
        }

        /// <summary>
        /// This prompt may be archived.
        /// If so, this means it is not a subject to send into LLM.
        /// </summary>
        public bool IsArchived
        {
            get;
            private set;
        }

        /// <summary>
        /// Приватный конструктор для обеспечения контроля над созданием объектов
        /// </summary>
        /// <param name="promptBody">Пользовательский ввод</param>
        private UserPrompt(string promptBody)
        {
            if (promptBody is null)
                throw new ArgumentNullException(nameof(promptBody));

            PromptBody = promptBody;
            Answer = new LLMAnswer();
        }

        public UserChatMessage CreateChatMessage()
        {
            return new UserChatMessage(
                ChatMessageContentPart.CreateTextPart(PromptBody)
                );
        }

        public void Archive()
        {
            IsArchived = true;
        }

        /// <summary>
        /// Создает текстовый промпт для общего обсуждения
        /// </summary>
        /// <param name="userPrompt">Пользовательский текст</param>
        /// <returns>Новый объект UserPrompt</returns>
        public static UserPrompt CreateTextBasedPrompt(string userPrompt) =>
            new UserPrompt(
                userPrompt
                );

        /// <summary>
        /// Создает промпт для предложения целой строки кода в указанной позиции
        /// </summary>
        /// <param name="userCodeFileName">Имя файла с кодом</param>
        /// <param name="documentText">Полный текст документа</param>
        /// <param name="caretPosition">Позиция курсора для вставки якоря</param>
        /// <returns>Новый объект UserPrompt</returns>
        public static async Task<UserPrompt> CreateSuggestWholeLineAsync(string userCodeFileName, string documentText, int caretPosition)
        {
            var fi = new FileInfo(userCodeFileName);
            var anchor = await "SuggestWholeLine_Anchor".GetLocalizedResourceByNameAsync();

            documentText = documentText.Insert(caretPosition, anchor);

            return new UserPrompt(
                await GetSuggestWholeLinePromptAsync()
                + Environment.NewLine
                + Environment.NewLine
                + "```"
                + LanguageHelper.GetMarkdownLanguageCodeBlockNameBasedOnFileExtension(fi.Extension)
                + Environment.NewLine
                + documentText
                + Environment.NewLine
                + "```"
            );
        }

        private static async Task<string> GetSuggestWholeLinePromptAsync()
        {
            var prompt = await "SuggestWholeLine".GetLocalizedResourceByNameAsync();
            var anchor = await "SuggestWholeLine_Anchor".GetLocalizedResourceByNameAsync();
            return string.Format(prompt, anchor);
        }
    }

    public sealed class LLMAnswer
    {
        private readonly List<OpenAI.Chat.ChatMessage> _reactions = new();
        private readonly StringBuilder _userVisibleAnswer = new();

        public bool IsEmpty => _reactions.Count == 0;

        public IReadOnlyList<OpenAI.Chat.ChatMessage> Reactions => _reactions;

        public void AddPermanentReaction(
            ToolChatMessage toolChatMessage
            )
        {
            _reactions.Add(toolChatMessage);
        }

        public void UpdateUserVisibleAnswer(
            string suffix
            )
        {
            _userVisibleAnswer.Append(suffix);
        }

        public void AddPermanentReaction(
            AssistantChatMessage assistantChatMessage
            )
         {
            _reactions.Add(assistantChatMessage);
        }

        public string GetUserVisibleAnswer()
        {
            return _userVisibleAnswer.ToString();
        }
    }

}
