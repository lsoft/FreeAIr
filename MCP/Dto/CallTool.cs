using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace Dto
{
    public sealed class CallToolRequest : BaseRequest
    {

        public string ToolName
        {
            get;
            set;
        }

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

    public sealed class CallToolReply : BaseReply
    {
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
