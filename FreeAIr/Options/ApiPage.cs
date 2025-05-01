using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FreeAIr
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class ApiPageOptions : BaseOptionPage<ApiPage>
        {
        }

        [ComVisible(true)]
        public class ResponsePageOptions : BaseOptionPage<ResponsePage>
        {
        }
    }

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
        [DisplayName("Switch to task window")]
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

    public class ApiPage : BaseOptionModel<ApiPage>
    {
        [Category("OpenAI compatible API requisites")]
        [DisplayName("Endpoint")]
        [Description("An endpoint of LLM API provider.")]
        [DefaultValue("https://openrouter.ai/api/v1")]
        public string Endpoint { get; set; } = "https://openrouter.ai/api/v1";

        [Category("OpenAI compatible API requisites")]
        [DisplayName("Token")]
        [Description("A token of LLM API provider.")]
        [DefaultValue("place your token here")]
        public string Token { get; set; } = "place your token here";

        [Category("OpenAI compatible API requisites")]
        [DisplayName("Chosen model")]
        [Description("A token of LLM API provider.")]
        [DefaultValue("qwen/qwen3-14b:free")]
        public string ChosenModel { get; set; } = "qwen/qwen3-14b:free";
    }

    public sealed class XshdProvider
    {
        public string LanguageAlias
        {
            get;
            set;
        }

        public string XshdFilePath
        {
            get;
            set;
        }
    }

    public enum LLMResultEnum
    {
        MD,
        PlainText
    }
}
