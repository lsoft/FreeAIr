namespace Dto
{
    /// <summary>Asks the proxy for the tool list of one MCP server - what populates the tool picker in the UI.</summary>
    public sealed class GetToolsRequest : BaseRequest
    {
        public GetToolsRequest()
        {
        }

        public GetToolsRequest(
            string mcpServerName,
            IReadOnlyDictionary<string, string>? parameters = null
            ) : base(mcpServerName, parameters)
        {
            
        }
    }

    /// <summary>The tools <see cref="GetToolsRequest"/> asked for, each still carrying its raw JSON schema so the caller can build the parameters for a later <see cref="CallToolRequest"/>.</summary>
    public sealed class GetToolsReply : BaseReply
    {
        public GetToolReply[] Tools
        {
            get;
            set;
        }

        public GetToolsReply()
        {
        }

        public GetToolsReply(
            GetToolReply[] tools
            )
        {
            if (tools is null)
            {
                throw new ArgumentNullException(nameof(tools));
            }

            Tools = tools;
        }

    }

    /// <summary>One tool as its MCP server describes it - name, human-readable description and its JSON Schema for parameters, all still opaque strings at this layer.</summary>
    public sealed class GetToolReply
    {
        public string Name
        {
            get;
            set;
        }

        public string Description
        {
            get;
            set;
        }

        /// <summary>The tool's input JSON Schema, as raw JSON text.</summary>
        public string Parameters
        {
            get;
            set;
        }

        public GetToolReply()
        {
        }

        public GetToolReply(string name, string description, string parameters)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
            }

            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentException($"'{nameof(description)}' cannot be null or empty.", nameof(description));
            }

            if (string.IsNullOrEmpty(parameters))
            {
                throw new ArgumentException($"'{nameof(parameters)}' cannot be null or empty.", nameof(parameters));
            }

            Name = name;
            Description = description;
            Parameters = parameters;
        }
    }
}
