using OpenAI.Chat;
using System.Collections.Generic;
using FreeAIr.Chat.Content;

namespace FreeAIr.Chat
{
    /// <summary>
    /// Контракт для пользовательских запросов к ИИ-ассистенту
    /// </summary>
    public sealed class UserPrompt : IChatContent
    {
        /// <summary>Marks this content as a user prompt, as opposed to an assistant answer or a tool reaction.</summary>
        public ChatContentTypeEnum Type => ChatContentTypeEnum.Prompt;

        /// <summary>The text the user typed into the prompt box.</summary>
        /// <inheritdoc />
        public string PromptBody
        {
            get;
        }

        /// <summary>True once <see cref="Archive"/> has been called, marking this prompt as no longer editable.</summary>
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

        /// <summary>Wraps the prompt text into a single user chat message ready for the request.</summary>
        public IReadOnlyList<ChatMessage> CreateChatMessages()
        {
            return
                [
                    new UserChatMessage(
                        ChatMessageContentPart.CreateTextPart(PromptBody)
                        )
                ];
        }

        /// <summary>Marks this prompt as archived, once it has been sent and superseded by the assistant's answer.</summary>
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
