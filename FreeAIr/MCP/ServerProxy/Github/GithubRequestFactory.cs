using Dto;
using FreeAIr.Options2;
using System.Collections.Generic;
using System.IO;

namespace FreeAIr.MCP.McpServerProxy.Github
{
    /// <summary>
    /// Builds the four requests <see cref="GithubMcpServerProxy"/> sends to the GitHub server, all
    /// stamped with the same per-call parameters: the folder the executable lives (or will be
    /// installed) in, and the user's GitHub token pulled fresh from options on every call.
    /// </summary>
    public static class GithubRequestFactory
    {
        public const string MCPServerFolderName = @"MCP\Server";

        /// <summary>Builds an <see cref="IsInstalledRequest"/> for the GitHub server.</summary>
        public static async System.Threading.Tasks.Task<IsInstalledRequest> IsInstalledRequestAsync(
            )
        {
            return new IsInstalledRequest(
                GithubMcpServerProxy.PublicMCPServerName,
                await GetMcpServerSpecificArgumentsAsync()
                );
        }

        /// <summary>Builds an <see cref="InstallRequest"/> for the GitHub server.</summary>
        public static async System.Threading.Tasks.Task<InstallRequest> InstallRequestAsync(
            )
        {
            return new InstallRequest(
                GithubMcpServerProxy.PublicMCPServerName,
                await GetMcpServerSpecificArgumentsAsync()
                );
        }

        /// <summary>Builds a <see cref="GetToolsRequest"/> for the GitHub server.</summary>
        public static async System.Threading.Tasks.Task<GetToolsRequest> GetToolsRequestAsync(
            )
        {
            return new GetToolsRequest(
                GithubMcpServerProxy.PublicMCPServerName,
                await GetMcpServerSpecificArgumentsAsync()
                );
        }

        /// <summary>Builds a <see cref="CallToolRequest"/> for one tool call on the GitHub server.</summary>
        public static async System.Threading.Tasks.Task<CallToolRequest> CallToolRequestAsync(
            string toolName,
            Dictionary<string, object?>? arguments
           )
        {
            if (toolName is null)
            {
                throw new ArgumentNullException(nameof(toolName));
            }

            return new CallToolRequest(
                GithubMcpServerProxy.PublicMCPServerName,
                toolName,
                arguments,
                await GetMcpServerSpecificArgumentsAsync()
                );
        }

        /// <summary>The folder the GitHub server executable is installed into (or would be), under the VSIX's working folder.</summary>
        public static string MCPServerFolderPath
        {
            get
            {
                return Path.Combine(
                    FreeAIrPackage.WorkingFolder,
                    MCPServerFolderName
                    );
            }
        }

        /// <summary>The parameters every GitHub server call needs: its folder path and a freshly read GitHub token.</summary>
        private static async System.Threading.Tasks.Task<Dictionary<string, string>> GetMcpServerSpecificArgumentsAsync()
        {
            return new Dictionary<string, string>
            {
                ["MCPServerFolderPath"] = MCPServerFolderPath,
                ["GitHubToken"] = (await FreeAIrOptions.DeserializeUnsortedAsync()).GetGitHubToken(),
            };
        }

    }
}
