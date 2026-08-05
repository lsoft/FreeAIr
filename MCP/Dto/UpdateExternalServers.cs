namespace Dto
{
    /// <summary>Pushes the VS side's current set of externally configured MCP servers down to the proxy, so its own copy of `mcpServers` stays in sync after the user edits it in the UI.</summary>
    public sealed class UpdateExternalServersRequest
    {
        public McpServers McpServers
        {
            get;
            set;
        }

        public UpdateExternalServersRequest()
        {
        }

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
        public McpServers McpServers
        {
            get;
            set;
        }

        public UpdateExternalServersReply()
        {
        }

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
