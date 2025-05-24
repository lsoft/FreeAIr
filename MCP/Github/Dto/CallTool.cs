using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace Dto
{
    public sealed class CallToolRequest
    {
        public string MCPServerFolderPath
        {
            get;
            set;
        }

        public string GithubToken
        {
            get;
            set;
        }

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
            string mCPServerFolderPath,
            string githubToken,
            string toolName,
            Dictionary<string, object?>? arguments
            )
        {
            if (string.IsNullOrEmpty(mCPServerFolderPath))
            {
                throw new ArgumentException($"'{nameof(mCPServerFolderPath)}' cannot be null or empty.", nameof(mCPServerFolderPath));
            }

            if (string.IsNullOrEmpty(githubToken))
            {
                throw new ArgumentException($"'{nameof(githubToken)}' cannot be null or empty.", nameof(githubToken));
            }

            if (string.IsNullOrEmpty(toolName))
            {
                throw new ArgumentException($"'{nameof(toolName)}' cannot be null or empty.", nameof(toolName));
            }

            MCPServerFolderPath = mCPServerFolderPath;
            GithubToken = githubToken;
            ToolName = toolName;
            Arguments = arguments;
        }
    }

    public sealed class CallToolReply : Reply
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
