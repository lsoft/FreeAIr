namespace FreeAIr.SetupWizard.Catalog
{
    /// <summary>
    /// The MCP servers FreeAIr can register by itself, as data: the name they are stored under and
    /// the endpoint they are reached at. Both the control center's install command and the setup
    /// wizard's opt-in checkbox read them from here, so the two cannot drift apart and a server
    /// registered by one is recognized as installed by the other.
    ///
    /// Only servers reachable over plain HTTP live here. The GitHub server is not one of them: it
    /// is a local binary that has to be downloaded first, which is a job for
    /// <c>GithubMcpServerProxy</c> rather than for a catalog entry.
    /// </summary>
    public static class KnownMcpServerCatalog
    {
        /// <summary>The name the Microsoft Learn documentation server is registered under in the options.</summary>
        public const string MicrosoftDocsServerName = "microsoft.docs.mcp";

        /// <summary>The well-known endpoint of the Microsoft Learn documentation MCP server.</summary>
        public const string MicrosoftDocsEndpoint = "https://learn.microsoft.com/api/mcp";

        /// <summary>
        /// The raw configuration JSON an HTTP MCP server entry carries. Written here rather than at
        /// each call site so that the shape the proxy expects is stated once.
        /// </summary>
        public static string BuildHttpConfiguration(string endpoint)
        {
            return
$@"{{
  ""type"": ""http"",
  ""url"": ""{endpoint}""
}}";
        }
    }
}
