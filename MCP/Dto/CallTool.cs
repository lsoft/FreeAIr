namespace Dto
{
    /// <summary>Invokes one tool of one MCP server with a specific set of arguments - the actual tool call, as opposed to <see cref="GetToolsRequest"/> which only lists what is callable.</summary>
    public sealed class CallToolRequest : BaseRequest
    {

        public string ToolName
        {
            get;
            set;
        }

        /// <summary>The tool's arguments, keyed by parameter name, matching its JSON Schema from <see cref="GetToolReply.Parameters"/>.</summary>
        public Dictionary<string, object?>? Arguments
        {
            get;
            set;
        }

        public CallToolRequest()
        {
        }

        public CallToolRequest(
            string mcpServerName,
            string toolName,
            Dictionary<string, object?>? arguments,
            IReadOnlyDictionary<string, string>? parameters = null
            ) : base(mcpServerName, parameters)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                throw new ArgumentException($"'{nameof(toolName)}' cannot be null or empty.", nameof(toolName));
            }

            ToolName = toolName;
            Arguments = arguments;
        }
    }

    /// <summary>The result of a <see cref="CallToolRequest"/> - the tool's output content, or an error flagged through <see cref="IsError"/> rather than an exception.</summary>
    public sealed class CallToolReply : BaseReply
    {
        /// <summary>True when the MCP server itself reported the call as failed - distinct from a transport-level failure, which surfaces through <see cref="BaseReply.ErrorMessage"/> instead.</summary>
        public bool IsError
        {
            get;
            set;
        }

        public string[] Content
        {
            get;
            set;
        }

        public CallToolReply()
        {
        }

        public CallToolReply(bool isError, string[] content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            IsError = isError;
            Content = content;
        }
    }

}
