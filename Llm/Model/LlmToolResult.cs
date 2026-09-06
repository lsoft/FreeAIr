namespace FreeAIr.Llm
{
    /// <summary>
    /// What a tool answered, carried back to the model so it can go on with the turn.
    ///
    /// A failure is reported as a result with <see cref="IsError"/> set rather than as a missing
    /// message: a tool call the model never hears the end of leaves the transcript unbalanced, and
    /// both protocols reject a request in that shape. Telling the model the tool failed is also
    /// what makes it try something else instead of asking for the same call again.
    /// </summary>
    public sealed class LlmToolResult
    {
        /// <summary>The id of the <see cref="LlmToolCall"/> this is the answer to.</summary>
        public string ToolCallId
        {
            get;
        }

        /// <summary>The tool's output as text, or the description of the failure when <see cref="IsError"/> is set.</summary>
        public string Text
        {
            get;
        }

        /// <summary>
        /// True when the tool did not run or threw. Anthropic has a flag of its own for this
        /// (`is_error` on the tool_result block); OpenAI has none and the text is all the model
        /// gets.
        /// </summary>
        public bool IsError
        {
            get;
        }

        /// <summary>Pairs a tool's output with the call it answers.</summary>
        public LlmToolResult(
            string toolCallId,
            string? text,
            bool isError = false
            )
        {
            if (toolCallId is null)
            {
                throw new ArgumentNullException(nameof(toolCallId));
            }

            ToolCallId = toolCallId;
            Text = text ?? string.Empty;
            IsError = isError;
        }
    }
}
