using FreeAIr.Shared.Helper;
using OpenAI.Chat;
using System.Collections.Generic;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Conversion helpers between the OpenAI SDK's streaming chat tool call updates and the
    /// finalized <see cref="ChatToolCall"/> type, used while assembling a streamed chat response
    /// into a complete tool call for the chat agent.
    /// </summary>
    public static class OpenAIHelper
    {
        /// <summary>
        /// Converts a batch of streamed tool call fragments, accumulated while a chat response
        /// is still arriving, into the finalized <see cref="ChatToolCall"/> list ready for execution.
        /// </summary>
        public static IReadOnlyList<ChatToolCall> ConvertToChatTools(
            this IReadOnlyList<StreamingChatToolCallUpdate> tcus
            )
        {
            return tcus.ConvertAll(tcu => tcu.ConvertToChatTool());
        }

        /// <summary>
        /// Converts a single streamed tool call update into a finalized function-style
        /// <see cref="ChatToolCall"/>, pairing its tool call id with the function name and arguments.
        /// </summary>
        public static ChatToolCall ConvertToChatTool(
            this StreamingChatToolCallUpdate tcu
            )
        {
            return
                ChatToolCall.CreateFunctionToolCall(
                    tcu.ToolCallId,
                    tcu.FunctionName,
                    tcu.FunctionArgumentsUpdate
                    );
        }
    }
}
