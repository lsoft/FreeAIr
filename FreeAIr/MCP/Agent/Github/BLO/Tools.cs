using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.MCP.Agent.Github.BLO
{
    public sealed class AgentTools
    {
        public IReadOnlyList<AgentTool> Tools
        {
            get;
        }

        public AgentTools(
            IReadOnlyList<AgentTool> tools
            )
        {
            if (tools is null)
            {
                throw new ArgumentNullException(nameof(tools));
            }

            Tools = tools;
        }
    }

    public sealed class AgentTool
    {
        public string Name
        {
            get;
        }

        public string Description
        {
            get;
        }

        public string Parameters
        {
            get;
        }

        public AgentTool(string name, string description, string parameters)
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

    public sealed class AgentToolCallResult
    {
        public string[] Content
        {
            get;
        }

        public AgentToolCallResult(string[] content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Content = content;
        }
    }
}
