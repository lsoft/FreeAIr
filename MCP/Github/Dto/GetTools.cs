using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace Dto
{
    public sealed class GetToolsRequest
    {
        public static readonly GetToolsRequest Instance = new();
    }

    public sealed class GetToolsReply : Reply
    {
        public string AgentName
        {
            get;
            set;
        }

        public GetToolReply[] Tools
        {
            get;
            set;
        }

        public GetToolsReply()
        {
        }

        public GetToolsReply(
            string agentName,
            GetToolReply[] tools
            )
        {
            if (string.IsNullOrEmpty(agentName))
            {
                throw new ArgumentException($"'{nameof(agentName)}' cannot be null or empty.", nameof(agentName));
            }

            if (tools is null)
            {
                throw new ArgumentNullException(nameof(tools));
            }

            AgentName = agentName;
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
