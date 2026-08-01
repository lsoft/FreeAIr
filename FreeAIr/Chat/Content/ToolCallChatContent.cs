using FreeAIr.Helper;
using OpenAI.Chat;
using System.Collections.Generic;

namespace FreeAIr.Chat.Content
{
    /// <summary>
    /// A tool the model asked to invoke, together with the outcome of that invocation.
    ///
    /// The lifecycle is Asking (waiting for the user's permission) → Executing → Succeeded /
    /// Failed / Blocked. A tool call which never leaves a terminal state would stall the whole
    /// dialogue, because the chat resumes the conversation only when every tool call of the turn
    /// is finished.
    /// </summary>
    public sealed class ToolCallChatContent : IChatContent
    {
        /// <summary>
        /// Tells the chat to check whether the turn can be continued.
        /// Supplied by <see cref="FreeAIr.Chat.Chat.CreateToolCall"/>.
        /// </summary>
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

        /// <summary>
        /// Records the terminal state of the call and lets the chat know it may go on.
        /// This is the only place from which the conversation is resumed after tool calls.
        /// </summary>
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
        /// <summary>
        /// The user is being asked whether this tool may run.
        /// </summary>
        Asking,

        Executing,

        Succeeded,

        /// <summary>
        /// The tool ran and threw. The failure is reported back to the model, which usually makes
        /// it try something else.
        /// </summary>
        Failed,

        /// <summary>
        /// The user refused to let the tool run.
        /// </summary>
        Blocked
    }

}
