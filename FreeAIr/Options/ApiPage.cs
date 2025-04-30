using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace FreeAIr
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class ApiPageOptions : BaseOptionPage<ApiPage>
        {
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
        [DefaultValue("")]
        public string Token { get; set; } = "";

        [Category("OpenAI compatible API requisites")]
        [DisplayName("Chosen model")]
        [Description("A token of LLM API provider.")]
        [DefaultValue("qwen/qwen3-14b:free")]
        public string ChosenModel { get; set; } = "qwen/qwen3-14b:free";

        [Category("Response")]
        [DisplayName("Type")]
        [Description("A type of response of LLM API provider.")]
        [TypeConverter(typeof(EnumConverter))]
        [DefaultValue(LLMResultEnum.MD)]
        public LLMResultEnum Result  { get; set; } = LLMResultEnum.MD;


    }

    public enum LLMResultEnum
    {
        MD,
        PlainText
    }
}
