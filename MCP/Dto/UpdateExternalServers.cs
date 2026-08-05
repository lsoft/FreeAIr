namespace Dto
{
    /// <summary>Pushes the VS side's current set of externally configured MCP servers down to the proxy, so its own copy of `mcpServers` stays in sync after the user edits it in the UI.</summary>
    public sealed class UpdateExternalServersRequest
    {
        /// <summary>The externally configured server set to push down to the proxy.</summary>
        public McpServers McpServers
        {
            get;
            set;
        }

        /// <summary>Parameterless constructor for JSON deserialization.</summary>
        public UpdateExternalServersRequest()
        {
        }

        /// <summary>Wraps the server set to be pushed to the proxy.</summary>
        public UpdateExternalServersRequest(
            McpServers mcpServers
            )
        {
            McpServers = mcpServers;
        }
    }

    /// <summary>Echoes back the server set the proxy ended up with, so the caller can confirm the update actually took.</summary>
    public sealed class UpdateExternalServersReply : BaseReply
    {
        /// <summary>The server set the proxy ended up with after applying the update.</summary>
        public McpServers McpServers
        {
            get;
            set;
        }

        /// <summary>Parameterless constructor for JSON deserialization.</summary>
        public UpdateExternalServersReply()
        {
        }

        /// <summary>Wraps the proxy's resulting server set.</summary>
        public UpdateExternalServersReply(
            McpServers mcpServers
            )
        {
            if (mcpServers is null)
            {
                throw new ArgumentNullException(nameof(mcpServers));
            }

            McpServers = mcpServers;
        }
    }
}
