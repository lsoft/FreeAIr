namespace Dto
{
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
