using Dto;
using FreeAIr.Options2;
using System.Collections.Generic;
using System.IO;

namespace FreeAIr.MCP.McpServerProxy.Github
{
    public static class GithubRequestFactory
    {
        public const string MCPServerFolderName = @"MCP\Server";

        public static async System.Threading.Tasks.Task<IsInstalledRequest> IsInstalledRequestAsync(
            )
        {
            return new IsInstalledRequest(
                GithubMcpServerProxy.PublicMCPServerName,
                await GetMcpServerSpecificArgumentsAsync()
                );
        }

        public static async System.Threading.Tasks.Task<InstallRequest> InstallRequestAsync(
            )
        {
            return new InstallRequest(
                GithubMcpServerProxy.PublicMCPServerName,
                await GetMcpServerSpecificArgumentsAsync()
                );
        }

        public static async System.Threading.Tasks.Task<GetToolsRequest> GetToolsRequestAsync(
            )
        {
            return new GetToolsRequest(
                GithubMcpServerProxy.PublicMCPServerName,
                await GetMcpServerSpecificArgumentsAsync()
                );
        }

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

        private static async System.Threading.Tasks.Task<Dictionary<string, string>> GetMcpServerSpecificArgumentsAsync()
        {
            return new Dictionary<string, string>
            {
                ["MCPServerFolderPath"] = MCPServerFolderPath,
                ["GitHubToken"] = (await FreeAIrOptions.DeserializeUnsortedAsync()).GitHubToken,
            };
        }

    }
}
