using Dto;
using ModelContextProtocol.Client;

namespace Proxy.Server.Github
{
    /// <summary>
    /// The one MCP server the proxy always registers itself: the official `github-mcp-server`,
    /// self-installed via <see cref="GithubInstaller"/> and launched as a stdio process with the
    /// user's PAT injected as its `GITHUB_PERSONAL_ACCESS_TOKEN` environment variable.
    /// </summary>
    public class GithubServer : BaseServer2<GithubServer>
    {
        public const string PublicMCPServerName = "github.com";

        public GithubServer()
        {
        }

        /// <summary>Whether the executable is already unpacked at the folder path the caller supplies as a parameter.</summary>
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

        /// <summary>Downloads and unpacks the executable via <see cref="GithubInstaller"/>.</summary>
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

        /// <summary>Launches the installed executable as a stdio MCP server, passing the caller's GitHub token through its environment; returns null if the executable is not installed yet.</summary>
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
