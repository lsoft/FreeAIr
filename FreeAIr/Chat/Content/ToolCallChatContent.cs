using FreeAIr.Helper;
using OpenAI.Chat;
using System.Collections.Generic;

namespace FreeAIr.Chat.Content
{
    public sealed class ToolCallChatContent : IChatContent
    {
        private readonly Action _checkForRequestAnswer;

        public ChatContentTypeEnum Type => ChatContentTypeEnum.ToolCall;

        public bool IsArchived
        {
            get;
            private set;
        }

        public ToolCallStatusEnum Status
        {
            get;
            private set;
        }

        public StreamingChatToolCallUpdate ToolCall
        {
            get;
        }

        public string Name => ToolCall.FunctionName;

        public string? Result
        {
            get;
            private set;
        }

        public ToolCallChatContent(
            StreamingChatToolCallUpdate toolCall,
            Action checkForRequestAnswer
            )
        {
            if (toolCall is null)
            {
                throw new ArgumentNullException(nameof(toolCall));
            }

            if (checkForRequestAnswer is null)
            {
                throw new ArgumentNullException(nameof(checkForRequestAnswer));
            }

            ToolCall = toolCall;
            Status = ToolCallStatusEnum.Asking;
            _checkForRequestAnswer = checkForRequestAnswer;
        }

        public void Archive()
        {
            IsArchived = true;
        }

        public void SetStatus(
            ToolCallStatusEnum status
            )
        {
            Status = status;
        }

        public void SetResult(
            ToolCallStatusEnum status,
            string? result
            )
        {
            Status = status;
            Result = result;

            _checkForRequestAnswer();
        }

        public IReadOnlyList<ChatMessage> CreateChatMessages()
        {
            var result = new List<ChatMessage>();

            var m1 = new AssistantChatMessage(
                [ ToolCall.ConvertToChatTool() ]
                );
            result.Add(m1);

            if (!string.IsNullOrEmpty(Result))
            {
                var m2 = new ToolChatMessage(
                    ToolCall.ToolCallId,
                    Result
                    );
                result.Add(m2);
            }

            return result;
        }
    }

    public enum ToolCallStatusEnum
    {
        Asking,
        Executing,
        Succeeded,
        Failed,
        Blocked
    }

}
