using FreeAIr.BuildErrors;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreeAIr.Chat.Context.Item;

namespace FreeAIr.Options2.Support
{
    /// <summary>
    /// Everything a support action prompt may ask to have filled in. The user writes the matching
    /// anchor, such as `{GIT_DIFF}`, into the prompt in the settings file, and
    /// <see cref="SupportContext"/> supplies the value.
    /// </summary>
    public enum SupportContextVariableEnum
    {
        /// <summary>An anchor which is not one of ours. Substituted with an empty string.</summary>
        Unknown,

        /// <summary>The files or selections the action was invoked on, as a comma separated list of paths.</summary>
        ContextItemName,

        /// <summary>Text of the compiler error the action was invoked from in the error list.</summary>
        BuildErrorMessage,

        /// <summary>Line the compiler error points at.</summary>
        BuildErrorLine,

        /// <summary>Column the compiler error points at.</summary>
        BuildErrorColumn,

        /// <summary>The test framework configured in the settings, so generated tests come out in the right one.</summary>
        UnitTestFramework,

        /// <summary>The staged diff, used by the action which writes a commit message.</summary>
        GitDiff,

        /// <summary>What the user typed into the natural language search box.</summary>
        NaturalLanguageSearchQuery,

        /// <summary>The marker string standing where the caret is, for whole line completion.</summary>
        WholeLineCompletionAnchor,

        /// <summary>What the microphone recording was transcribed into.</summary>
        RecordedText
    }

    /// <summary>
    /// Maps the anchor strings the user writes in the settings file to the variables the code knows
    /// about, and back. The spelling of every anchor lives here and nowhere else.
    /// </summary>
    public static class SupportContextVariableHelper
    {
        private const string ContextItemName = "{CONTEXT_ITEM_NAME}";
        private const string BuildErrorMessage = "{BUILD_ERROR_MESSAGE}";
        private const string BuildErrorLine = "{BUILD_ERROR_LINE}";
        private const string BuildErrorColumn = "{BUILD_ERROR_COLUMN}";
        private const string UnitTestFramework = "{UNIT_TEST_FRAMEWORK}";
        private const string GitDiff = "{GIT_DIFF}";
        private const string NaturalLanguageSearchQuery = "{NATURAL_LANGUAGE_SEARCH_QUERY}";
        private const string WholeLineCompletionAnchor = "{WHOLE_LINE_COMPLETION_ANCHOR}";
        private const string RecordedText = "{RECORDED_TEXT}";

        /// <summary>
        /// Every anchor, in the order they are substituted. This is also the list the prompt editor
        /// offers for completion, so an anchor missing from here is invisible to the user.
        /// </summary>
        public static readonly string[] Anchors =
            [
                ContextItemName,
                BuildErrorMessage,
                BuildErrorLine,
                BuildErrorColumn,
                UnitTestFramework,
                GitDiff,
                NaturalLanguageSearchQuery,
                WholeLineCompletionAnchor,
                RecordedText
            ];

        /// <summary>
        /// The variable an anchor stands for, or <see cref="SupportContextVariableEnum.Unknown"/>
        /// for anything unrecognized — a prompt may legitimately contain braces of its own.
        /// </summary>
        public static SupportContextVariableEnum GetVariableEnum(
            string anchor
            )
        {
            switch (anchor)
            {
                case ContextItemName:
                    return SupportContextVariableEnum.ContextItemName;
                case BuildErrorMessage:
                    return SupportContextVariableEnum.BuildErrorMessage;
                case BuildErrorLine:
                    return SupportContextVariableEnum.BuildErrorLine;
                case BuildErrorColumn:
                    return SupportContextVariableEnum.BuildErrorColumn;
                case UnitTestFramework:
                    return SupportContextVariableEnum.UnitTestFramework;
                case GitDiff:
                    return SupportContextVariableEnum.GitDiff;
                case NaturalLanguageSearchQuery:
                    return SupportContextVariableEnum.NaturalLanguageSearchQuery;
                case WholeLineCompletionAnchor:
                    return SupportContextVariableEnum.WholeLineCompletionAnchor;
                case RecordedText:
                    return SupportContextVariableEnum.RecordedText;
            }

            return SupportContextVariableEnum.Unknown;
        }

        /// <summary>
        /// The anchor text of a variable. Used to compose the shipped default prompts in code, so
        /// they and the settings file speak the same language.
        /// </summary>
        public static string GetAnchor(
            this SupportContextVariableEnum variable
            )
        {
            switch (variable)
            {
                case SupportContextVariableEnum.ContextItemName:
                    return ContextItemName;
                case SupportContextVariableEnum.BuildErrorMessage:
                    return BuildErrorMessage;
                case SupportContextVariableEnum.BuildErrorLine:
                    return BuildErrorLine;
                case SupportContextVariableEnum.BuildErrorColumn:
                    return BuildErrorColumn;
                case SupportContextVariableEnum.UnitTestFramework:
                    return UnitTestFramework;
                case SupportContextVariableEnum.GitDiff:
                    return GitDiff;
                case SupportContextVariableEnum.NaturalLanguageSearchQuery:
                    return NaturalLanguageSearchQuery;
                case SupportContextVariableEnum.WholeLineCompletionAnchor:
                    return WholeLineCompletionAnchor;
                case SupportContextVariableEnum.RecordedText:
                    return RecordedText;
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// The values to substitute into a support action prompt.
    ///
    /// Support action prompts are written by the user in the json settings and contain anchors
    /// like `{GIT_DIFF}` or `{BUILD_ERROR_MESSAGE}`. Every place that triggers an action builds a
    /// context with the `With...Async` factory that matches it, and the anchors it knows about get
    /// their values; the rest are replaced with an empty string, so a prompt written for one scope
    /// never breaks when it is reused in another.
    ///
    /// The preferred unit test framework is added by every factory, because "generate unit tests"
    /// can be invoked from almost anywhere.
    /// </summary>
    public sealed class SupportContext
    {
        /// <summary>Only the variables this particular context knows. Everything else resolves to empty.</summary>
        private Dictionary<SupportContextVariableEnum, string> _contextVariables = new();

        /// <summary>What has been collected, for the prompt preview shown before an action is sent.</summary>
        public IReadOnlyDictionary<SupportContextVariableEnum, string> ContextVariables => _contextVariables;

        /// <summary>
        /// Records one value, overwriting any previous one. Called by the factories below rather
        /// than from the outside.
        /// </summary>
        public void AddContextVariable(
            SupportContextVariableEnum variable,
            string value
            )
        {
            _contextVariables[variable] = value;
        }

        /// <summary>
        /// Replaces every known anchor in the prompt with its value from this context, or with an
        /// empty string when this context has nothing for it.
        /// </summary>
        public string ApplyVariablesToPrompt(
            string prompt
            )
        {
            foreach (var anchor in SupportContextVariableHelper.Anchors)
            {
                prompt = prompt.Replace(
                    anchor,
                    GetVariableValue(anchor)
                    );
            }

            return prompt;
        }


        /// <summary>
        /// An empty context, for the actions invoked from the prompt box where the user has already
        /// written everything themselves and no anchor has a value.
        /// </summary>
        public static SupportContext WithPrompt()
        {
            var result = new SupportContext();

            return result;
        }

        /// <summary>For the voice actions: carries what the microphone recording was transcribed into.</summary>
        public static async Task<SupportContext> WithRecordedTextAsync(
            string recordedText
            )
        {
            if (recordedText is null)
            {
                throw new ArgumentNullException(nameof(recordedText));
            }

            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            var result = new SupportContext();

            result.AddContextVariable(
                SupportContextVariableEnum.RecordedText,
                recordedText
                );
            result.AddContextVariable(
                SupportContextVariableEnum.UnitTestFramework,
                unsorted.PreferredUnitTestFramework
                );

            return result;
        }

        /// <summary>For the commit message action: carries the staged diff the message is to describe.</summary>
        public static async Task<SupportContext> WithGitDiffAsync(
            string gitDiff
            )
        {
            if (gitDiff is null)
            {
                throw new ArgumentNullException(nameof(gitDiff));
            }

            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            var result = new SupportContext();

            result.AddContextVariable(
                SupportContextVariableEnum.GitDiff,
                gitDiff
                );
            result.AddContextVariable(
                SupportContextVariableEnum.UnitTestFramework,
                unsorted.PreferredUnitTestFramework
                );

            return result;
        }

        /// <summary>For the natural language search: carries the query the user typed into the search box.</summary>
        public static async Task<SupportContext> WithNaturalLanguageSearchQueryAsync(
            string searchQuery
            )
        {
            if (searchQuery is null)
            {
                throw new ArgumentNullException(nameof(searchQuery));
            }

            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            var result = new SupportContext();

            result.AddContextVariable(
                SupportContextVariableEnum.NaturalLanguageSearchQuery,
                searchQuery
                );
            result.AddContextVariable(
                SupportContextVariableEnum.UnitTestFramework,
                unsorted.PreferredUnitTestFramework
                );

            return result;
        }

        /// <summary>
        /// For the actions invoked on chat context items: names every attached file, quoted, in one
        /// comma separated list.
        ///
        /// Overloaded with the one taking a plain name; both end up as the same node in the outline
        /// index, so nothing that matters may live in this summary alone.
        /// </summary>
        public static async Task<SupportContext> WithContextItemAsync(
            IReadOnlyList<SolutionItemChatContextItem> contextItems
            )
        {
            if (contextItems is null)
            {
                throw new ArgumentNullException(nameof(contextItems));
            }

            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            var result = new SupportContext();

            result.AddContextVariable(
                SupportContextVariableEnum.ContextItemName,
                string.Join(", ", contextItems.Select(s => $"`{s.SelectedIdentifier.FilePath}`"))
                );
            result.AddContextVariable(
                SupportContextVariableEnum.UnitTestFramework,
                unsorted.PreferredUnitTestFramework
                );

            return result;
        }

        /// <summary>
        /// For the actions invoked on the solution tree: names the selected projects and files by
        /// their full paths.
        /// </summary>
        public static async Task<SupportContext> WithSolutionItemsAsync(
            List<SolutionItem> solutionItems
            )
        {
            if (solutionItems is null)
            {
                throw new ArgumentNullException(nameof(solutionItems));
            }

            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            var result = new SupportContext();

            result.AddContextVariable(
                SupportContextVariableEnum.ContextItemName,
                string.Join(", ", solutionItems.Select(s => $"`{s.FullPath}`"))
                );
            result.AddContextVariable(
                SupportContextVariableEnum.UnitTestFramework,
                unsorted.PreferredUnitTestFramework
                );

            return result;
        }

        /// <summary>
        /// For the actions which know nothing but a name — a document path, an MCP tool result. See
        /// the overload taking context items.
        /// </summary>
        public static async Task<SupportContext> WithContextItemAsync(
            string contextItemName
            )
        {
            if (contextItemName is null)
            {
                throw new ArgumentNullException(nameof(contextItemName));
            }

            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            var result = new SupportContext();

            result.AddContextVariable(
                SupportContextVariableEnum.ContextItemName,
                $"`{contextItemName}`"
                );
            result.AddContextVariable(
                SupportContextVariableEnum.UnitTestFramework,
                unsorted.PreferredUnitTestFramework
                );

            return result;
        }

        /// <summary>
        /// For the fix build error action: carries the message, the file and the exact line and
        /// column the compiler complained about.
        /// </summary>
        public static async Task<SupportContext> WithErrorInformationAsync(
            BuildResultInformation errorInformation
            )
        {
            if (errorInformation is null)
            {
                throw new ArgumentNullException(nameof(errorInformation));
            }

            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            var result = new SupportContext();

            result.AddContextVariable(
                SupportContextVariableEnum.ContextItemName,
                "`" + errorInformation.FilePath + "`"
                );
            result.AddContextVariable(
                SupportContextVariableEnum.BuildErrorMessage,
                errorInformation.ErrorDescription
                );
            result.AddContextVariable(
                SupportContextVariableEnum.BuildErrorLine,
                errorInformation.Line.ToString()
                );
            result.AddContextVariable(
                SupportContextVariableEnum.BuildErrorColumn,
                errorInformation.Column.ToString()
                );
            result.AddContextVariable(
                SupportContextVariableEnum.UnitTestFramework,
                unsorted.PreferredUnitTestFramework
                );

            return result;
        }

        /// <summary>
        /// For whole line completion: names the document and the marker string which stands where
        /// the caret is, so the prompt can ask what belongs in its place.
        /// </summary>
        public static async Task<SupportContext> WithWholeLineDataAsync(
            string contextItemName
            )
        {
            if (contextItemName is null)
            {
                throw new ArgumentNullException(nameof(contextItemName));
            }

            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            var result = new SupportContext();

            result.AddContextVariable(
                SupportContextVariableEnum.ContextItemName,
                $"`{contextItemName}`"
                );
            result.AddContextVariable(
                SupportContextVariableEnum.WholeLineCompletionAnchor,
                unsorted.WholeLineCompletionAnchorName
                );
            result.AddContextVariable(
                SupportContextVariableEnum.UnitTestFramework,
                unsorted.PreferredUnitTestFramework
                );

            return result;
        }

        /// <summary>
        /// The value behind an anchor, or an empty string when this context does not have it. Empty
        /// rather than an error, so a prompt written for one scope stays usable in another.
        /// </summary>
        private string GetVariableValue(
            string anchor
            )
        {
            var variable = SupportContextVariableHelper.GetVariableEnum(anchor);
            if (!_contextVariables.TryGetValue(variable, out var value))
            {
                return string.Empty;
            }

            return value;
        }

    }
}
