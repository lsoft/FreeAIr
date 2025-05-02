using System.ComponentModel;

namespace FreeAIr
{
    public class ResponsePage : BaseOptionModel<ResponsePage>
    {
        private XshdProvider[] _xshdProviders =
            [
                new XshdProvider
                {
                    LanguageAlias = "cs,csharp",
                    XshdFilePath = @"Resources/xshd/csharp.xshd"
                }
            ];

        [Category("Response")]
        [DisplayName("Type")]
        [Description("A type of response of LLM API provider.")]
        [TypeConverter(typeof(EnumConverter))]
        [DefaultValue(LLMResultEnum.MD)]
        public LLMResultEnum ResponseFormat  { get; set; } = LLMResultEnum.MD;

        [Category("Response")]
        [DisplayName("Switch to chat window")]
        [Description("Should FreeAIr switch to its window after dev asked a prompt.")]
        [DefaultValue(true)]
        public bool SwitchToTaskWindow { get; set; } = true;

        [Category("Response")]
        [DisplayName("Code colorization schemas")]
        [Description("Edit this list to provide custom colorization schemas for your programming languages. Xshd file path can be local relative of FreeAIr.dll, or absolute. RESTART OF VISUAL STUDIO IS REQUIRED TO APPLY THIS OPTION.")]
        [DefaultValue(true)]
        public XshdProvider[] XshdProviders
        {
            get => _xshdProviders;
            set
            {
                _xshdProviders = value;

                LoadOrUpdateMarkdownStyles();
            }
        }

        [Category("Response")]
        [DisplayName("Custom culture of AI responses")]
        [Description("If your preferred AI answers culture is differ of your VS UI culture, then use this option to override AI answer culture.")]
        [DefaultValue("")]
        public string OverriddenCulture { get; set; } = "";

        public static void LoadOrUpdateMarkdownStyles()
        {
            System.Windows.Application.Current.Resources.Remove("MdXamlPlugins");

            var plugins = new MdXaml.Plugins.MdXamlPlugins();
            foreach (var xshdProvider in ResponsePage.Instance.XshdProviders)
            {
                var fullPath = System.IO.Path.IsPathRooted(xshdProvider.XshdFilePath)
                    ? xshdProvider.XshdFilePath
                    : System.IO.Path.GetFullPath(
                        System.IO.Path.Combine(
                            FreeAIrPackage.WorkingFolder,
                            xshdProvider.XshdFilePath
                            )
                        )
                    ;

                plugins.Highlights.Add(
                    new MdXaml.Plugins.Definition
                    {
                        Alias = xshdProvider.LanguageAlias,
                        Resource = new Uri("file:///" + fullPath)
                    }
                );
            }

            System.Windows.Application.Current.Resources.Add("MdXamlPlugins", plugins);
        }

    }
}
