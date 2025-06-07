using Dto;
using System.Collections.Generic;
using System.IO;

namespace FreeAIr.MCP.McpServerProxy.Github
{
    public static class GithubRequestFactory
    {
        public const string MCPServerFolderName = @"MCP\Server";

        public static IsInstalledRequest IsInstalledRequest(
            )
        {
            return new IsInstalledRequest(
                GithubMcpServerProxy.PublicMCPServerName,
                GetMcpServerSpecificArguments()
                );
        }

        public static InstallRequest InstallRequest(
            )
        {
            return new InstallRequest(
                GithubMcpServerProxy.PublicMCPServerName,
                GetMcpServerSpecificArguments()
                );
        }

        public static GetToolsRequest GetToolsRequest(
            )
        {
            return new GetToolsRequest(
                GithubMcpServerProxy.PublicMCPServerName,
                GetMcpServerSpecificArguments()
                );
        }

        public static CallToolRequest CallToolRequest(
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
                GetMcpServerSpecificArguments()
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

        private static Dictionary<string, string> GetMcpServerSpecificArguments()
        {
            return new Dictionary<string, string>
            {
                ["MCPServerFolderPath"] = MCPServerFolderPath,
                ["GitHubToken"] = MCPPage.Instance.GitHubToken,
            };
        }

    }
}
