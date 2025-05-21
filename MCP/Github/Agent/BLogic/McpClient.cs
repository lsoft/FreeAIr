using ModelContextProtocol.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent.BLogic
{
    public static class McpClient
    {
        public const string PublicMCPServerName = "github.com";

        private static IMcpClient? _mcpClient = null;

        public static async Task<IMcpClient?> CreateClientAsync(
            string mcpServerFolderPath,
            string githubToken
            )
        {
            if (_mcpClient is not null)
            {
                return _mcpClient;
            }

            if (!Installer.IsInstalled(mcpServerFolderPath))
            {
                return null;
            }

            var mcpClientTransport = new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Name = PublicMCPServerName,
                    WorkingDirectory = mcpServerFolderPath,
                    Command = Installer.ExeFileName,
                    ShutdownTimeout = TimeSpan.FromMinutes(1),
                    Arguments = ["stdio", /*"--read-only"*/ ],
                    EnvironmentVariables = new Dictionary<string, string?>
                    {
                        ["GITHUB_PERSONAL_ACCESS_TOKEN"] = githubToken
                    }
                }
                );
            _mcpClient = await McpClientFactory.CreateAsync(mcpClientTransport);

            return _mcpClient;
        }

    }
}
