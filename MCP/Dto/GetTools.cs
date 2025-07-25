﻿namespace Dto
{
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
