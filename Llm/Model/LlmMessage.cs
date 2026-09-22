using System.Collections.Generic;

namespace FreeAIr.Llm
{
    /// <summary>
    /// One entry of the transcript sent to the model, in the shape both protocols can be built
    /// from: a role, optional text, the tool calls an assistant turn asked for, and the result of
    /// a tool call.
    ///
    /// Deliberately not one class per role. The transcript is assembled by the chat contents, which
    /// know nothing about either wire format, and every difference between the two - a tool result
    /// being its own role or a block inside a user message, an assistant turn carrying text and
    /// tool calls together or apart - is settled by the transport rather than by the shape of this
    /// list. Use the named factory methods; the constructor is private because only four
    /// combinations of these fields mean anything.
    /// </summary>
    public sealed class LlmMessage
    {
        /// <summary>Who this message is from.</summary>
        public LlmRole Role
        {
            get;
        }

        /// <summary>The text of the message, or null when it carries only tool calls or a tool result.</summary>
        public string? Text
        {
            get;
        }

        /// <summary>The tool calls an assistant turn asked for; empty for every other role.</summary>
        public IReadOnlyList<LlmToolCall> ToolCalls
        {
            get;
        }

        /// <summary>The answer of a tool, set on <see cref="LlmRole.Tool"/> messages only.</summary>
        public LlmToolResult? ToolResult
        {
            get;
        }

        private LlmMessage(
            LlmRole role,
            string? text,
            IReadOnlyList<LlmToolCall>? toolCalls,
            LlmToolResult? toolResult
            )
        {
            Role = role;
            Text = text;
            ToolCalls = toolCalls ?? Array.Empty<LlmToolCall>();
            ToolResult = toolResult;
        }

        /// <summary>
        /// Something the user said, or material FreeAIr attached on their behalf - a context
        /// document, a selection, the copilot instructions.
        /// </summary>
        public static LlmMessage CreateUserMessage(
            string text
            )
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return new LlmMessage(LlmRole.User, text, null, null);
        }

        /// <summary>Plain text the model answered with.</summary>
        public static LlmMessage CreateAssistantMessage(
            string text
            )
        {
            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            return new LlmMessage(LlmRole.Assistant, text, null, null);
        }

        /// <summary>
        /// The assistant turn which asked for tools. The text is optional because a model may
        /// reason aloud before calling one, and both protocols carry that text in the same message
        /// as the calls.
        /// </summary>
        public static LlmMessage CreateAssistantToolCallMessage(
            IReadOnlyList<LlmToolCall> toolCalls,
            string? text = null
            )
        {
            if (toolCalls is null)
            {
                throw new ArgumentNullException(nameof(toolCalls));
            }

            return new LlmMessage(LlmRole.Assistant, text, toolCalls, null);
        }

        /// <summary>The answer of one tool, to be paired with the call of the same id.</summary>
        public static LlmMessage CreateToolResultMessage(
            LlmToolResult toolResult
            )
        {
            if (toolResult is null)
            {
                throw new ArgumentNullException(nameof(toolResult));
            }

            return new LlmMessage(LlmRole.Tool, null, null, toolResult);
        }
    }
}
