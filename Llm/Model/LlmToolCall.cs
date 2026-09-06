namespace FreeAIr.Llm
{
    /// <summary>
    /// One invocation the model asked for: which tool, with which arguments, under which id.
    ///
    /// The arguments stay raw JSON text from the moment they leave the wire until the moment the
    /// MCP server receives them. Reviving them into a dictionary of objects in between is what
    /// issue #70 was about, and the same rule holds here: a nested object or array survives a
    /// round trip through weakly typed values in neither protocol.
    /// </summary>
    public sealed class LlmToolCall
    {
        /// <summary>
        /// The id the provider gave this call, echoed back with its result so the model can pair
        /// the two. OpenAI calls it `tool_call.id`, Anthropic `tool_use.id`; both are opaque.
        /// </summary>
        public string Id
        {
            get;
        }

        /// <summary>
        /// The fully qualified `Server.Tool` name, exactly as it was advertised in
        /// <see cref="LlmToolDefinition.Name"/>.
        /// </summary>
        public string Name
        {
            get;
        }

        /// <summary>
        /// The arguments as a JSON object in text form. Never empty: a call which carries no
        /// arguments is written as `{}`, because an empty string is not a JSON object and the
        /// endpoints reject the transcript that contains one.
        /// </summary>
        public string ArgumentsJson
        {
            get;
        }

        /// <summary>
        /// Builds a complete tool call. Arguments which are null or empty become `{}` here, so
        /// that no caller downstream has to repeat that check.
        /// </summary>
        public LlmToolCall(
            string id,
            string name,
            string? argumentsJson
            )
        {
            if (id is null)
            {
                throw new ArgumentNullException(nameof(id));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
            }

            Id = id;
            Name = name;
            ArgumentsJson = string.IsNullOrWhiteSpace(argumentsJson)
                ? "{}"
                : argumentsJson!;
        }
    }
}
