using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
