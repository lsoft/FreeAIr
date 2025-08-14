using OpenAI.Chat;
using System.Collections.Generic;
using System.Linq;

namespace FreeAIr.MCP.McpServerProxy
{
    public sealed class McpServerTools
    {
        public IReadOnlyList<McpServerTool> Tools
        {
            get;
        }

        public McpServerTools(
            IReadOnlyList<McpServerTool> tools
            )
        {
            if (tools is null)
            {
                throw new ArgumentNullException(nameof(tools));
            }

            Tools = tools;
        }
    }

    public /*sealed*/ class McpServerTool
    {
        public string McpServerProxyName
        {
            get;
        }

        public string ToolName
        {
            get;
        }

        public string FullName => McpServerProxyName + "." + ToolName;

        public string Description
        {
            get;
        }

        public string Parameters
        {
            get;
        }

        public McpServerTool(
            string mcpServerProxyName,
            string toolName,
            string description,
            string parameters
            )
        {
            if (string.IsNullOrEmpty(mcpServerProxyName))
            {
                throw new ArgumentException($"'{nameof(mcpServerProxyName)}' cannot be null or empty.", nameof(mcpServerProxyName));
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

            McpServerProxyName = mcpServerProxyName;
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

    public sealed class McpServerProxyToolCallResult
    {
        public McpServerProxyToolCallResultEnum Result
        {
            get;
        }

        public string[] Content
        {
            get;
        }

        public McpServerProxyToolCallResult(
            McpServerProxyToolCallResultEnum result,
            string[] content
            )
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            Result = result;
            Content = content;
        }

        public static McpServerProxyToolCallResult CreateSuccess(
            IEnumerable<string> content
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Success,
                content.ToArray()
                );
        }

        public static McpServerProxyToolCallResult CreateSuccess(
            string content
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Success,
                [ content ]
                );
        }

        public static McpServerProxyToolCallResult CreateFailed(
            string errorMessage
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Fail,
                [ errorMessage ]
                );
        }

        public static McpServerProxyToolCallResult CreatePostponed(
            )
        {
            return new McpServerProxyToolCallResult(
                McpServerProxyToolCallResultEnum.Postpone,
                [string.Empty]
                );
        }
    }

    public enum McpServerProxyToolCallResultEnum
    {
        Success,
        Fail,
        Postpone
    }
}
