using FreeAIr.Helper;
using OpenAI.Chat;
using System.Collections.Generic;
using System.Text;

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

        /// <summary>Always <see cref="ChatContentTypeEnum.ToolCall"/>.</summary>
        public ChatContentTypeEnum Type => ChatContentTypeEnum.ToolCall;

        /// <summary>
        /// Whether this tool call has been dropped from the history sent to the model while
        /// staying visible in the chat window.
        /// </summary>
        public bool IsArchived
        {
            get;
            private set;
        }

        /// <summary>Where this call is in its Asking → Executing → terminal-state lifecycle.</summary>
        public ToolCallStatusEnum Status
        {
            get;
            private set;
        }

        /// <summary>The raw tool invocation as streamed from the model (name and arguments).</summary>
        public StreamingChatToolCallUpdate ToolCall
        {
            get;
        }

        /// <summary>The name of the tool the model asked to run.</summary>
        public string Name => ToolCall.FunctionName;

        /// <summary>The tool's output once the call has finished, or null while it is still pending.</summary>
        public string? Result
        {
            get;
            private set;
        }

        /// <summary>
        /// Rebuilds a tool call from a saved chat file. A call that was still running when Visual
        /// Studio closed cannot be resumed, so it lands as failed; one that was waiting for
        /// permission stays asking, and the user can allow or block it again.
        /// </summary>
        public static ToolCallChatContent Restore(
            string? toolCallId,
            string? functionName,
            string? arguments,
            int index,
            ToolCallStatusEnum status,
            string? result,
            bool isArchived,
            Action checkForRequestAnswer
            )
        {
            if (checkForRequestAnswer is null)
            {
                throw new ArgumentNullException(nameof(checkForRequestAnswer));
            }

            var restoredStatus = status;
            var restoredResult = result;
            if (restoredStatus == ToolCallStatusEnum.Executing)
            {
                restoredStatus = ToolCallStatusEnum.Failed;
                restoredResult = "Interrupted when Visual Studio closed.";
            }

            var toolCall = OpenAIChatModelFactory.StreamingChatToolCallUpdate(
                index: index,
                toolCallId: toolCallId ?? string.Empty,
                kind: ChatToolCallKind.Function,
                functionName: functionName ?? string.Empty,
                functionArgumentsUpdate: BinaryData.FromString(
                    string.IsNullOrEmpty(arguments) ? "{}" : arguments
                    )
                );

            var content = new ToolCallChatContent(toolCall, checkForRequestAnswer)
            {
                Status = restoredStatus,
                Result = restoredResult,
            };

            if (isArchived)
            {
                content.Archive();
            }

            return content;
        }

        /// <summary>Wraps a streamed tool call request in the Asking state, pending user permission.</summary>
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

        /// <summary>Drops this tool call out of the history sent to the model.</summary>
        public void Archive()
        {
            IsArchived = true;
        }

        /// <summary>Moves this call to a new, non-terminal lifecycle state (e.g. Executing).</summary>
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

        /// <summary>
        /// Renders this call as the assistant message announcing it, followed by the tool result
        /// message once one is available.
        /// </summary>
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

    /// <summary>Lifecycle states a <see cref="ToolCallChatContent"/> moves through.</summary>
    public enum ToolCallStatusEnum
    {
        /// <summary>
        /// The user is being asked whether this tool may run.
        /// </summary>
        Asking,

        /// <summary>The tool is currently running.</summary>
        Executing,

        /// <summary>The tool ran and returned a result.</summary>
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
