using Dto;
using System.Collections.Generic;
using System.IO;

namespace FreeAIr.MCP.Agent.Github
{
    public static class GithubRequestFactory
    {
        public const string MCPServerFolderName = @"MCP\Server";

        public static IsInstalledRequest IsInstalledRequest(
            )
        {
            return new IsInstalledRequest(
                GithubAgent.PublicMCPServerName,
                GetAgentSpecificArguments()
                );
        }

        public static InstallRequest InstallRequest(
            )
        {
            return new InstallRequest(
                GithubAgent.PublicMCPServerName,
                GetAgentSpecificArguments()
                );
        }

        public static GetToolsRequest GetToolsRequest(
            )
        {
            return new GetToolsRequest(
                GithubAgent.PublicMCPServerName,
                GetAgentSpecificArguments()
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
                GithubAgent.PublicMCPServerName,
                toolName,
                arguments,
                GetAgentSpecificArguments()
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

        private static Dictionary<string, string> GetAgentSpecificArguments()
        {
            return new Dictionary<string, string>
            {
                ["MCPServerFolderPath"] = MCPServerFolderPath,
                ["GitHubToken"] = MCPPage.Instance.GitHubToken,
            };
        }

    }
}
