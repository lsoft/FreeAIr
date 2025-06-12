using System.ComponentModel;
using System.Globalization;

namespace FreeAIr
{
    public class ResponsePage : BaseOptionModel<ResponsePage>
    {
        [Category("Response")]
        [DisplayName("Max output token count")]
        [Description("Maximum count of tokens LLM answer can contain.")]
        [DefaultValue(8192)]
        public int MaxOutputTokenCount { get; set; } = 8192;

        [Category("Response")]
        [DisplayName("Switch to chat window")]
        [Description("Should FreeAIr switch to its window after dev asked a prompt.")]
        [DefaultValue(true)]
        public bool SwitchToTaskWindow { get; set; } = true;

        [Category("Response")]
        [DisplayName("Custom culture of AI responses")]
        [Description("If your preferred AI answers culture is differ of your VS UI culture, then use this option to override AI answer culture. For example set ru-RU to get answers in Russian.")]
        [DefaultValue("")]
        public string OverriddenCulture { get; set; } = "";

        [Category("Response")]
        [DisplayName("Implicit whole line completion")]
        [Description("If you are using a free model, it means slow response (a few seconds) and daily limit for prompt count. In this case you do not want FreeAIr make whole line completion prompts automatically. If so, keep this in 'False', you still able to invoke this explicitly with Alt+A.")]
        [DefaultValue(false)]
        public bool IsImplicitWholeLineCompletionEnabled { get; set; } = false;


        [Category("Response")]
        [DisplayName("Preferred unit test framework")]
        [Description("Set your preferred unit test framework. This is used for unit tests generation.")]
        [DefaultValue("XUnit")]
        public string PreferredUnitTestFramework { get; set; } = "XUnit";

        [Category("Response")]
        [DisplayName("Timeout for automatic searching chat context items in msec.")]
        [Description("Set this timeout (in msec) to determine a time spent for automatic searching context items by code dependencies.")]
        [DefaultValue(500)]
        public int AutomaticSearchForContextItemsTimeoutMsec { get; set; } = 1500;

        public static CultureInfo GetAnswerCulture()
        {
            return
                string.IsNullOrEmpty(ResponsePage.Instance.OverriddenCulture)
                    ? CultureInfo.CurrentUICulture
                    : CultureInfo.CreateSpecificCulture(ResponsePage.Instance.OverriddenCulture)
                    ;
        }

        public static string GetAnswerCultureName()
        {
            return
                string.IsNullOrEmpty(ResponsePage.Instance.OverriddenCulture)
                    ? CultureInfo.CurrentUICulture.Name
                    : ResponsePage.Instance.OverriddenCulture
                    ;
        }
    }
}
