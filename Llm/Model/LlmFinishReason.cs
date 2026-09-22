namespace FreeAIr.Llm
{
    /// <summary>
    /// Why the model stopped. The chat cares about exactly one distinction - whether tool calls are
    /// waiting to be run - but the rest is worth keeping apart, because a turn cut off by the
    /// output limit looks to the user like a model which trails off mid-sentence.
    /// </summary>
    public enum LlmFinishReason
    {
        /// <summary>The endpoint did not say, or said something neither protocol defines.</summary>
        Unknown,

        /// <summary>The model finished what it had to say. OpenAI `stop`, Anthropic `end_turn`.</summary>
        Stop,

        /// <summary>
        /// The model asked for tools and is waiting for their results. This is what makes the
        /// tool-calling loop go round. OpenAI `tool_calls`, Anthropic `tool_use`.
        /// </summary>
        ToolCalls,

        /// <summary>
        /// The answer hit the output token limit. OpenAI `length`, Anthropic `max_tokens`.
        /// Worth reporting: the fix is a setting the user owns, not a retry.
        /// </summary>
        Length,

        /// <summary>The provider's own filter cut the answer off.</summary>
        ContentFilter
    }
}
