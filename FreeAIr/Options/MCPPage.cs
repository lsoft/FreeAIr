using System.ComponentModel;

namespace FreeAIr
{
    public class MCPPage : BaseOptionModel<MCPPage>
    {
        [Category("Servers")]
        [DisplayName("github.com server token")]
        [Description("A github.com MCP server token.")]
        [DefaultValue("place your github.com token here")]
        public string GitHubToken { get; set; } = "place your github.com token here";

        [Category("Servers")]
        [DisplayName("External MCP server")]
        [Description("A Claude compatible json for external MCP servers.")]
        [DefaultValue("place your github.com token here")]
        [Browsable(false)]
        public string ExternalMCPServers
        {
            get; set;
        } =
$$$"""
{
  "mcpServers": {

  }
}
""";

        [Category("Tools")]
        [DisplayName("AvailableTools")]
        [Description("An json with list of available tools and its status.")]
        [Browsable(false)]
        [DefaultValue("")]
        public string AvailableTools { get; set; } = string.Empty;

    }
}
