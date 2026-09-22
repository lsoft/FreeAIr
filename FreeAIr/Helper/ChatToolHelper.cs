using FreeAIr.Llm;
using System.Collections.Generic;
using System.Text.Json;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Turns the raw JSON arguments of a tool call into a plain dictionary, so MCP tool invocations
    /// do not have to parse <see cref="JsonDocument"/> themselves.
    /// </summary>
    public static class ChatToolHelper
    {
        /// <summary>
        /// Parses the arguments of a completed tool call into a name-to-value dictionary. The
        /// values keep their JSON shape - a nested object stays an object - because the MCP channel
        /// carries them on as raw JSON and flattening them here is what issue #70 was about.
        /// </summary>
        public static Dictionary<string, object?> ParseToolInvocationArguments(
            this LlmToolCall toolCall
            )
        {
            if (toolCall is null)
            {
                throw new ArgumentNullException(nameof(toolCall));
            }

            var toolArguments = new Dictionary<string, object?>();

            using JsonDocument toolArgumentJson = JsonDocument.Parse(
                toolCall.ArgumentsJson
                );

            if (toolArgumentJson.RootElement.ValueKind != JsonValueKind.Object)
            {
                return toolArguments;
            }

            foreach (var pair in toolArgumentJson.RootElement.EnumerateObject())
            {
                toolArguments.Add(pair.Name, pair.Value.DeserializeToObject());
            }

            return toolArguments;
        }
    }
}
