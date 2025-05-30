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
        public string ExternalMCPServers
        {
            get; set;
        } =
$$$"""
{
  "mcpServers": {
    "mssqlserver": {
      "command": "docker",
      "args": [
        "run",
        "--rm",
        "-i",
        "-e", "MSSQL_CONNECTIONSTRING=Server=192.168.1.121;User Id=clickhouse;Password=clickhouse;TrustServerCertificate=True;",
        "-e", "EnableExecuteQuery=true",
        "-e", "EnableExecuteStoredProcedure=true",
        "aadversteeg/mssqlclient-mcp-server:latest"
        ]
    }
  }
}
""";
    }
}
