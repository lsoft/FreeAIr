namespace FreeAIr.Llm
{
    /// <summary>
    /// One tool as advertised to the model: a name, a sentence saying what it does, and a JSON
    /// schema of its arguments.
    ///
    /// The schema stays text here rather than becoming a parsed object, because it comes from an
    /// MCP server unseen and is passed on unaltered apart from the normalization in
    /// <see cref="Wire.ToolSchemaNormalizer"/>. Where it goes in the request differs between the
    /// protocols - OpenAI puts it in `function.parameters`, Anthropic in `input_schema` - and that
    /// is the transport's business.
    /// </summary>
    public sealed class LlmToolDefinition
    {
        /// <summary>The name the model uses to call this tool; FreeAIr publishes MCP tools as `Server.Tool`.</summary>
        public string Name
        {
            get;
        }

        /// <summary>What the tool does, in the words its MCP server described it with.</summary>
        public string Description
        {
            get;
        }

        /// <summary>The JSON schema of the arguments, as an object schema in text form.</summary>
        public string ParametersJson
        {
            get;
        }

        /// <summary>Wraps a tool's advertised name, description and argument schema.</summary>
        public LlmToolDefinition(
            string name,
            string description,
            string parametersJson
            )
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
            }

            if (description is null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            if (parametersJson is null)
            {
                throw new ArgumentNullException(nameof(parametersJson));
            }

            Name = name;
            Description = description;
            ParametersJson = parametersJson;
        }
    }
}
