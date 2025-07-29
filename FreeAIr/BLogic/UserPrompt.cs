using OpenAI.Chat;
using System.Collections.Generic;
using System.Text;

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
