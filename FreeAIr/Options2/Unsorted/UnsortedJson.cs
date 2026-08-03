using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;

namespace FreeAIr.Options2.Unsorted
{
    /// <summary>
    /// Settings that have not yet found a dedicated node of their own: output length, answer
    /// culture, the unit test framework used for generation, the GitHub MCP token and the whole
    /// line completion anchor. Everything here is a leftover from before the settings were split
    /// into per-feature nodes such as <see cref="FreeAIr.Options2.Rag.RagJson"/>.
    /// </summary>
    [JsonConverter(typeof(JsonDescriptionCommentConverter<UnsortedJson>))]
    public sealed class UnsortedJson : ICloneable
    {
        /// <summary>Maximum count of tokens the LLM answer can contain.</summary>
        [Description("Maximum count of tokens LLM answer can contain.")]
        public int MaxOutputTokenCount
        {
            get;
            set;
        } = 8192;

        /// <summary>Overrides the answer culture when it should differ from the Visual Studio UI culture, e.g. `ru-RU` for Russian answers.</summary>
        [Description("If your preferred AI answers culture is differ of your VS UI culture, then use this option to override AI answer culture. For example set ru-RU to get answers in Russian.")]
        public string OverriddenCulture
        {
            get;
            set;
        } = "";

        /// <summary>The unit test framework named in generated tests.</summary>
        [Description("Set your preferred unit test framework. This is used for unit tests generation.")]
        public string PreferredUnitTestFramework
        {
            get;
            set;
        } = "XUnit";

        /// <summary>How long, in milliseconds, the automatic search for context items by code dependencies is allowed to run before it gives up.</summary>
        [Description("Set this timeout (in msec) to determine a time spent for automatic searching context items by code dependencies.")]
        public int AutomaticSearchForContextItemsTimeoutMsec
        {
            get;
            set;
        } = 1500;

        /// <summary>
        /// The token for the github.com MCP server, resolved through <see cref="GetGitHubToken"/>:
        /// a value of the form `{$MY_VAR}` is read from that environment variable instead, which is
        /// what lets a settings file be committed without leaking the real token.
        /// </summary>
        [Description("A secret token for github.com MCP server. You can store here your token directly, or you can store your token in MY_VAR environment variable, and use here {$MY_VAR} value to retrieve your token from env vars. Do not forget to restart VS after setting the env var.")]
        public string GitHubToken
        {
            get;
            set;
        } = "{$MY_GITHUB_TOKEN}";


        /// <summary>The fill-in-the-middle anchor whole line completion inserts, which depends on the model in use.</summary>
        [Description("An anchor name for whole line completion logic. It may depend of your model.")]
        public string WholeLineCompletionAnchorName
        {
            get;
            set;
        } = "<｜fim_hole｜>";

        /// <summary>Resolves <see cref="GitHubToken"/>, reading it out of an environment variable when it is written as `{$MY_VAR}`.</summary>
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

        /// <summary>
        /// Whether whole line completion fires automatically as the user types. Turned off by
        /// default for a free model, since those tend to answer slowly and carry a daily prompt
        /// limit; explicit invocation with Alt+A still works regardless of this setting.
        /// </summary>
        [Description("If you are using a free model, it usually means slow response (a few seconds) and daily limit for prompt count. In this case you do not want FreeAIr make whole line completion prompts automatically. If so, keep this in 'False', you still able to invoke this explicitly with Alt+A.")]
        public bool IsImplicitWholeLineCompletionEnabled
        {
            get;
            set;
        } = false;

        //the three RagTopOutlineCount / RagMaxFileCount / RagMinScore knobs used to live here and
        //have moved into the `Rag` node of the settings, where they are joined by the calibration
        //the threshold is now derived from

        public UnsortedJson()
        {
        }

        /// <inheritdoc/>
        public object Clone()
        {
            return new UnsortedJson
            {
                MaxOutputTokenCount = MaxOutputTokenCount,
                OverriddenCulture = OverriddenCulture,
                PreferredUnitTestFramework = PreferredUnitTestFramework,
                AutomaticSearchForContextItemsTimeoutMsec = AutomaticSearchForContextItemsTimeoutMsec,
                GitHubToken = GitHubToken,
                WholeLineCompletionAnchorName = WholeLineCompletionAnchorName,
                IsImplicitWholeLineCompletionEnabled = IsImplicitWholeLineCompletionEnabled,
            };
        }

        /// <summary>The culture answers are generated in: <see cref="OverriddenCulture"/> when set, otherwise the current VS UI culture.</summary>
        public CultureInfo GetAnswerCulture()
        {
            return
                string.IsNullOrEmpty(OverriddenCulture)
                    ? CultureInfo.CurrentUICulture
                    : CultureInfo.CreateSpecificCulture(OverriddenCulture)
                    ;
        }

        /// <summary>Same as <see cref="GetAnswerCulture"/>, as the raw culture name.</summary>
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
