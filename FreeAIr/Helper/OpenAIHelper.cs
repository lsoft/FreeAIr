using FreeAIr.Shared.Helper;
using OpenAI.Chat;
using System.Collections.Generic;

namespace FreeAIr.Helper
{
    public static class OpenAIHelper
    {
        public static IReadOnlyList<ChatToolCall> ConvertToChatTools(
            this IReadOnlyList<StreamingChatToolCallUpdate> tcus
            )
        {
            return tcus.ConvertAll(tcu => tcu.ConvertToChatTool());
        }

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
