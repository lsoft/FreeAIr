using FreeAIr.BLogic.Context.Item;
using FreeAIr.BuildErrors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Options2.Support
{
    public enum SupportContextVariableEnum
    {
        Unknown,
        ContextItemName,
        BuildErrorMessage,
        BuildErrorLine,
        BuildErrorColumn,
        UnitTestFramework,
        GitDiff,
        NaturalLanguageSearchQuery
    }

    public static class SupportContextVariableHelper
    {
        private const string ContextItemName = "{CONTEXT_ITEM_NAME}";
        private const string BuildErrorMessage = "{BUILD_ERROR_MESSAGE}";
        private const string BuildErrorLine = "{BUILD_ERROR_LINE}";
        private const string BuildErrorColumn = "{BUILD_ERROR_COLUMN}";
        private const string UnitTestFramework = "{UNIT_TEST_FRAMEWORK}";
        private const string GitDiff = "{GIT_DIFF}";
        private const string NaturalLanguageSearchQuery = "{NATURAL_LANGUAGE_SEARCH_QUERY}";

        public static readonly string[] Anchors =
            [
                ContextItemName,
                BuildErrorMessage,
                BuildErrorLine,
                BuildErrorColumn,
                UnitTestFramework,
                GitDiff,
                NaturalLanguageSearchQuery
            ];

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
            }

            return SupportContextVariableEnum.Unknown;
        }

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
            }

            return string.Empty;
        }
    }

    public sealed class SupportContext
    {
        private Dictionary<SupportContextVariableEnum, string> _contextVariables = new();

        public IReadOnlyDictionary<SupportContextVariableEnum, string> ContextVariables => _contextVariables;

        public void AddContextVariable(
            SupportContextVariableEnum variable,
            string value
            )
        {
            _contextVariables[variable] = value;
        }

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


        public static SupportContext WithPrompt()
        {
            var result = new SupportContext();

            return result;
        }

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
