using Dto;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Agent.Server.Github
{
    public class GithubServer : BaseServer2<GithubServer>
    {
        public const string PublicMCPServerName = "github.com";

        public GithubServer()
        {
        }

        protected override Task<IsInstalledReply> IsInstalledInternalAsync(
            IParameterProvider parameterProvider
            )
        {
            var result = GithubInstaller.IsInstalled(
                parameterProvider["MCPServerFolderPath"]
                );

            return Task.FromResult(
                new IsInstalledReply(result)
                );
        }

        protected override async Task<InstallReply> InstallInternalAsync(
            IParameterProvider parameterProvider
            )
        {
            var mcpServerFolderPath = parameterProvider["MCPServerFolderPath"];

            var result = await GithubInstaller.InstallAsync(
                mcpServerFolderPath
                );
            if (!result)
            {
                _log.Error("Cannot during installation");
                return BaseReply.FromError<InstallReply>("Error during installation");
            }

            return new InstallReply();
        }

        protected override async Task<IMcpClient?> CreateMcpClientAsync(
            IParameterProvider parameterProvider
            )
        {
            var mcpServerFolderPath = parameterProvider["MCPServerFolderPath"];

            if (!GithubInstaller.IsInstalled(mcpServerFolderPath))
            {
                return null;
            }

            var githubToken = parameterProvider["githubToken"];

            var mcpClientTransport = new StdioClientTransport(
                new StdioClientTransportOptions
                {
                    Name = PublicMCPServerName,
                    WorkingDirectory = mcpServerFolderPath,
                    Command = GithubInstaller.ExeFileName,
                    ShutdownTimeout = TimeSpan.FromMinutes(1),
                    Arguments = ["stdio", /*"--read-only"*/ ],
                    EnvironmentVariables = new Dictionary<string, string?>
                    {
                        ["GITHUB_PERSONAL_ACCESS_TOKEN"] = githubToken
                    }
                }
                );
            return await McpClientFactory.CreateAsync(mcpClientTransport);
        }
    }
}
