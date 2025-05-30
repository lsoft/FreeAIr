using System;
using System.Collections.Generic;
using System.Text;

namespace Dto
{
    public sealed class AddExternalServersRequest
    {
        public McpServers McpServers
        {
            get;
            set;
        }

        public AddExternalServersRequest()
        {
        }

        public AddExternalServersRequest(
            McpServers mcpServers
            )
        {
            McpServers = mcpServers;
        }
    }

    public sealed class AddExternalServersReply : BaseReply
    {
    }
}
