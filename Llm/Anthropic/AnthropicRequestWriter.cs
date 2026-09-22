using FreeAIr.Llm.Wire;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FreeAIr.Llm.Anthropic
{
    /// <summary>
    /// Writes the JSON body of a `POST /v1/messages` request.
    ///
    /// Separate from the transport so the request can be asserted without a server: what this class
    /// produces is the whole of what FreeAIr says in this protocol.
    /// </summary>
    internal static class AnthropicRequestWriter
    {
        /// <summary>
        /// Serializes one request. `max_tokens` is always written - the API makes it mandatory -
        /// and `tools`/`tool_choice` are omitted entirely rather than sent empty, because a choice
        /// with nothing to choose from is a validation error here.
        /// </summary>
        public static string Write(
            LlmRequest request,
            int defaultMaxOutputTokens
            )
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();

                writer.WriteString("model", request.Model);
                writer.WriteNumber(
                    "max_tokens",
                    request.MaxOutputTokens.HasValue && request.MaxOutputTokens.Value > 0
                        ? request.MaxOutputTokens.Value
                        : defaultMaxOutputTokens
                    );
                writer.WriteBoolean("stream", true);

                if (!string.IsNullOrEmpty(request.SystemPrompt))
                {
                    //a field of the request, not a message: this protocol has no system role
                    writer.WriteString("system", request.SystemPrompt);
                }

                WriteTools(writer, request);
                WriteMessages(writer, request);

                writer.WriteEndObject();
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        /// <summary>Writes the offered tools and how freely the model may reach for them.</summary>
        private static void WriteTools(
            Utf8JsonWriter writer,
            LlmRequest request
            )
        {
            if (request.Tools.Count == 0 || request.ToolChoice == LlmToolChoice.None)
            {
                return;
            }

            writer.WriteStartArray("tools");
            foreach (var tool in request.Tools)
            {
                writer.WriteStartObject();
                writer.WriteString("name", tool.Name);
                writer.WriteString("description", tool.Description);

                writer.WritePropertyName("input_schema");
                WriteRawJson(
                    writer,
                    ToolSchemaNormalizer.Normalize(tool.ParametersJson),
                    $"the input schema of tool {tool.Name}"
                    );

                writer.WriteEndObject();
            }
            writer.WriteEndArray();

            writer.WriteStartObject("tool_choice");
            writer.WriteString("type", "auto");
            writer.WriteEndObject();
        }

        /// <summary>Writes the transcript as rearranged by <see cref="AnthropicTranscript"/>.</summary>
        private static void WriteMessages(
            Utf8JsonWriter writer,
            LlmRequest request
            )
        {
            writer.WriteStartArray("messages");

            foreach (var message in AnthropicTranscript.Build(request.Messages))
            {
                writer.WriteStartObject();
                writer.WriteString("role", message.Role);

                writer.WriteStartArray("content");
                foreach (var block in message.Blocks)
                {
                    WriteBlock(writer, block);
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        /// <summary>Writes one content block in the shape its kind calls for.</summary>
        private static void WriteBlock(
            Utf8JsonWriter writer,
            AnthropicTranscript.Block block
            )
        {
            writer.WriteStartObject();

            switch (block.Kind)
            {
                case AnthropicTranscript.BlockKind.Text:
                    writer.WriteString("type", "text");
                    writer.WriteString("text", block.Text);
                    break;

                case AnthropicTranscript.BlockKind.ToolUse:
                    writer.WriteString("type", "tool_use");
                    writer.WriteString("id", block.ToolCallId);
                    writer.WriteString("name", block.ToolName);
                    //the arguments are the real object here, not the JSON *string* the other
                    //protocol carries - passing the text through would send a quoted blob
                    writer.WritePropertyName("input");
                    WriteRawJson(
                        writer,
                        block.ArgumentsJson,
                        $"the arguments of tool call {block.ToolCallId} ({block.ToolName})"
                        );
                    break;

                case AnthropicTranscript.BlockKind.ToolResult:
                    writer.WriteString("type", "tool_result");
                    writer.WriteString("tool_use_id", block.ToolCallId);
                    writer.WriteString("content", block.Text);
                    if (block.IsError)
                    {
                        writer.WriteBoolean("is_error", true);
                    }
                    break;
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Writes an already serialized JSON object as a value. Anything which does not parse, or
        /// parses to something other than an object, is written as an empty object: both places
        /// this is used - a tool's schema and a call's arguments - are object-valued members whose
        /// content came from somewhere FreeAIr does not control.
        /// </summary>
        private static void WriteRawJson(
            Utf8JsonWriter writer,
            string? json,
            string what
            )
        {
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    using var document = JsonDocument.Parse(json!);
                    if (document.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        document.RootElement.WriteTo(writer);
                        return;
                    }

                    //a tool which is offered with no arguments, or invoked with none, is a
                    //plausible report of "the tool did nothing" and invisible without this line
                    LlmDiagnostics.Report(
                        $"An empty object was sent in place of {what}: the json is a "
                        + $"{document.RootElement.ValueKind} rather than an object."
                        );
                }
                catch (JsonException excp)
                {
                    LlmDiagnostics.Report(
                        $"An empty object was sent in place of {what}: the json does not parse.",
                        excp
                        );
                }
            }

            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }
}
