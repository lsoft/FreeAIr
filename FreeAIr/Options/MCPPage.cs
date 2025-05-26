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

    }

    public class InternalPage : BaseOptionModel<InternalPage>
    {
        [Category("Tools")]
        [DisplayName("AvailableTools")]
        [Description("An json with list of available tools and its status.")]
        [Browsable(false)]
        [DefaultValue("")]
        public string AvailableTools { get; set; } = string.Empty;

        [Category("Logic")]
        [DisplayName("FreeAIr Last Version")]
        [DefaultValue("2.0.0")]
        [Browsable(false)]
        public string FreeAIrLastVersion
        {
            get;
            set;
        }

    }
}
