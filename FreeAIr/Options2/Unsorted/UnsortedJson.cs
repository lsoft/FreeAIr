using FreeAIr.Helper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FreeAIr.Options2.Unsorted
{
    [JsonConverter(typeof(JsonDescriptionCommentConverter<UnsortedJson>))]
    public sealed class UnsortedJson : ICloneable
    {
        [Description("Maximum count of tokens LLM answer can contain.")]
        public int MaxOutputTokenCount
        {
            get;
            set;
        } = 8192;

        [Description("Should FreeAIr switch to its window after dev asked a prompt.")]
        public bool SwitchToTaskWindow
        {
            get;
            set;
        } = true;

        [Description("If your preferred AI answers culture is differ of your VS UI culture, then use this option to override AI answer culture. For example set ru-RU to get answers in Russian.")]
        public string OverriddenCulture
        {
            get;
            set;
        } = "";

        [Description("Set your preferred unit test framework. This is used for unit tests generation.")]
        public string PreferredUnitTestFramework
        {
            get;
            set;
        } = "XUnit";

        [Description("Set this timeout (in msec) to determine a time spent for automatic searching context items by code dependencies.")]
        public int AutomaticSearchForContextItemsTimeoutMsec
        {
            get;
            set;
        } = 1500;

        [Description("A secret token for github.com MCP server. You can store here your token directly, or you can store your token in MY_VAR environment variable, and use here {$MY_VAR} value to retrieve your token from env vars. Do not forget to restart VS after setting the env var.")]
        public string GitHubToken
        {
            get;
            set;
        } = "place your github.com token here";

        public string GetGitHubToken()
        {
            if (GitHubToken.StartsWith("{$") && GitHubToken.EndsWith("}"))
            {
                //it's a env var!
                var varName = GitHubToken.Substring(2, GitHubToken.Length - 3);
                var result = Environment.GetEnvironmentVariable(varName);
                return result;
            }

            return GitHubToken;
        }

        [Description("If you are using a free model, it means slow response (a few seconds) and daily limit for prompt count. In this case you do not want FreeAIr make whole line completion prompts automatically. If so, keep this in 'False', you still able to invoke this explicitly with Alt+A.")]
        public bool IsImplicitWholeLineCompletionEnabled
        {
            get;
            set;
        } = false;

        public UnsortedJson()
        {
        }

        public object Clone()
        {
            return new UnsortedJson
            {
                MaxOutputTokenCount = MaxOutputTokenCount,
                SwitchToTaskWindow = SwitchToTaskWindow,
                OverriddenCulture = OverriddenCulture,
                PreferredUnitTestFramework = PreferredUnitTestFramework,
                AutomaticSearchForContextItemsTimeoutMsec = AutomaticSearchForContextItemsTimeoutMsec,
                GitHubToken = GitHubToken,
                IsImplicitWholeLineCompletionEnabled = IsImplicitWholeLineCompletionEnabled,
            };
        }

        public CultureInfo GetAnswerCulture()
        {
            return
                string.IsNullOrEmpty(OverriddenCulture)
                    ? CultureInfo.CurrentUICulture
                    : CultureInfo.CreateSpecificCulture(OverriddenCulture)
                    ;
        }

        public string GetAnswerCultureName()
        {
            return
                string.IsNullOrEmpty(OverriddenCulture)
                    ? CultureInfo.CurrentUICulture.Name
                    : OverriddenCulture
                    ;
        }
    }
}
