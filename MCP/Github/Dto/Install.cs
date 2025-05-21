using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dto
{
    public sealed class InstallRequest
    {
        public string MCPServerFolderPath
        {
            get;
            set;
        }

        public InstallRequest()
        {
        }

        public InstallRequest(
            string mcpServerFolderPath
            )
        {
            if (mcpServerFolderPath is null)
            {
                throw new ArgumentNullException(nameof(mcpServerFolderPath));
            }

            MCPServerFolderPath = mcpServerFolderPath;
        }
    }

    public sealed class InstallReply : Reply
    {
        public static readonly InstallReply Instance = new();
    }

    public sealed class IsInstalledRequest
    {
        public string MCPServerFolderPath
        {
            get;
            set;
        }

        public IsInstalledRequest()
        {
        }

        public IsInstalledRequest(
            string mcpServerFolderPath
            )
        {
            if (mcpServerFolderPath is null)
            {
                throw new ArgumentNullException(nameof(mcpServerFolderPath));
            }

            MCPServerFolderPath = mcpServerFolderPath;
        }
    }

    public sealed class IsInstalledReply : Reply
    {
        public bool IsInstalled
        {
            get;
            set;
        }

        public IsInstalledReply()
        {
        }

        public IsInstalledReply(bool isInstalled)
        {
            IsInstalled = isInstalled;
        }
    }
}
