using FreeAIr.BLogic.Content;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Text;

namespace FreeAIr.BLogic
{
    /// <summary>
    /// Контракт для пользовательских запросов к ИИ-ассистенту
    /// </summary>
    public sealed class UserPrompt : IChatContent
    {
        public ChatContentTypeEnum Type => ChatContentTypeEnum.Prompt;

        /// <inheritdoc />
        public string PromptBody
        {
            get;
        }

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
        }

        public ChatMessage CreateChatMessage()
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

    //public sealed class LLMAnswer
    //{
    //    private readonly List<OpenAI.Chat.ChatMessage> _reactions = new();
    //    private readonly StringBuilder _userVisibleAnswer = new();

    //    public bool IsEmpty => _reactions.Count == 0;

    //    public IReadOnlyList<OpenAI.Chat.ChatMessage> Reactions => _reactions;

    //    #region permanent reaction

    //    public void AddPermanentReaction(
    //        ToolChatMessage toolChatMessage
    //        )
    //    {
    //        _reactions.Add(toolChatMessage);
    //    }

    //    public void AddPermanentReaction(
    //        AssistantChatMessage assistantChatMessage
    //        )
    //    {
    //        _reactions.Add(assistantChatMessage);
    //    }

    //    #endregion

    //    #region user visible answer

    //    public void UpdateUserVisibleAnswer(
    //        string suffix
    //        )
    //    {
    //        _userVisibleAnswer.Append(suffix);
    //    }

    //    public string GetUserVisibleAnswer()
    //    {
    //        return _userVisibleAnswer.ToString();
    //    }

    //    #endregion
    //}

}
