using OpenAI.Chat;
using System.Collections.Generic;
using System.Text.Json;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Turns the raw JSON arguments of a streamed OpenAI tool call into a plain dictionary, so MCP
    /// tool invocations do not have to parse <see cref="JsonDocument"/> themselves.
    /// </summary>
    public static class ChatToolHelper
    {
        /// <summary>
        /// Parses the accumulated function-arguments JSON of a streaming tool call into a
        /// name-to-value dictionary.
        /// </summary>
        public static Dictionary<string, object?> ParseToolInvocationArguments(
            this StreamingChatToolCallUpdate toolCall
            )
        {
            var toolArguments = new Dictionary<string, object?>();
            if (toolCall.FunctionArgumentsUpdate.Length > 0)
            {
                using JsonDocument toolArgumentJson = JsonDocument.Parse(
                    toolCall.FunctionArgumentsUpdate
                    );

                foreach (var pair in toolArgumentJson.RootElement.EnumerateObject())
                {
                    toolArguments.Add(pair.Name, pair.Value.DeserializeToObject());
                }
            }

            return toolArguments;
        }
    }
}
