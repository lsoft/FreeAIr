namespace Dto
{
    /// <summary>Asks the proxy for the tool list of one MCP server - what populates the tool picker in the UI.</summary>
    public sealed class GetToolsRequest : BaseRequest
    {
        /// <summary>Parameterless constructor for JSON deserialization.</summary>
        public GetToolsRequest()
        {
        }

        /// <summary>Creates a request for the tool list of <paramref name="mcpServerName"/>.</summary>
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
        /// <summary>The server's callable tools, in the order the server reported them.</summary>
        public GetToolReply[] Tools
        {
            get;
            set;
        }

        /// <summary>Parameterless constructor for JSON deserialization.</summary>
        public GetToolsReply()
        {
        }

        /// <summary>Wraps the resolved tool list.</summary>
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
        /// <summary>The tool's identifier, as passed to <see cref="CallToolRequest.ToolName"/> to invoke it.</summary>
        public string Name
        {
            get;
            set;
        }

        /// <summary>Human-readable description of what the tool does, as the MCP server reports it.</summary>
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

        /// <summary>Parameterless constructor for JSON deserialization.</summary>
        public GetToolReply()
        {
        }

        /// <summary>Describes one callable tool: its name, description and input schema.</summary>
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
