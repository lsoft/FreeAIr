using OpenAI.Chat;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.MCP.Agent
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

    public /*sealed*/ class AgentTool
    {
        public string AgentName
        {
            get;
        }

        public string ToolName
        {
            get;
        }

        public string FullName => AgentName + "." + ToolName;

        public string Description
        {
            get;
        }

        public string Parameters
        {
            get;
        }

        public AgentTool(
            string agentName,
            string toolName,
            string description,
            string parameters
            )
        {
            if (string.IsNullOrEmpty(agentName))
            {
                throw new ArgumentException($"'{nameof(agentName)}' cannot be null or empty.", nameof(agentName));
            }

            if (string.IsNullOrEmpty(toolName))
            {
                throw new ArgumentException($"'{nameof(toolName)}' cannot be null or empty.", nameof(toolName));
            }

            if (string.IsNullOrEmpty(description))
            {
                throw new ArgumentException($"'{nameof(description)}' cannot be null or empty.", nameof(description));
            }

            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            AgentName = agentName;
            ToolName = toolName;
            Description = description;
            Parameters = parameters;

            if (FullName.Contains(' '))
            {
                throw new InvalidOperationException($"Function '{FullName}' contain spaces which is not allowed to some LLM providers.");
            }
        }

        public ChatTool CreateChatTool()
        {
            return ChatTool.CreateFunctionTool(
                functionName: FullName,
                functionDescription: Description,
                functionParameters: BinaryData.FromString(
                    Parameters
                    ),
                functionSchemaIsStrict: true
                );
        }
    }

    public sealed class AgentToolCallResult
    {
        public string[] Content
        {
            get;
        }

        public AgentToolCallResult(
            string content
            )
        {
            Content = [content];
        }

        public AgentToolCallResult(string[] content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Content = content;
        }

        public AgentToolCallResult(IEnumerable<string> content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Content = content.ToArray();
        }
    }
}
