using OpenAI.Chat;
using System.Collections.Generic;
using System.Text.Json;

namespace FreeAIr.Helper
{
    public static  class ChatToolHelper
    {
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
