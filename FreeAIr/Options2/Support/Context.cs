using FreeAIr.BuildErrors;
using SharpCompress.Common;
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
        ContextItemFilePath,
        BuildErrorMessage,
        BuildErrorLine,
        BuildErrorColumn,
        UnitTestFramework
    }

    public static class SupportContextVariableHelper
    {
        private const string ContextItemFilePath = "{CONTEXT_ITEM_FILEPATH}";
        private const string BuildErrorMessage = "{BUILD_ERROR_MESSAGE}";
        private const string BuildErrorLine = "{BUILD_ERROR_LINE}";
        private const string BuildErrorColumn = "{BUILD_ERROR_COLUMN}";
        private const string UnitTestFramework = "{UNIT_TEST_FRAMEWORK}";

        public static readonly string[] Anchors =
            [
                ContextItemFilePath,
                BuildErrorMessage,
                BuildErrorLine,
                BuildErrorColumn,
                UnitTestFramework,
            ];

        public static SupportContextVariableEnum GetVariableEnum(
            string anchor
            )
        {
            switch (anchor)
            {
                case ContextItemFilePath:
                    return SupportContextVariableEnum.ContextItemFilePath;
                case BuildErrorMessage:
                    return SupportContextVariableEnum.BuildErrorMessage;
                case BuildErrorLine:
                    return SupportContextVariableEnum.BuildErrorLine;
                case BuildErrorColumn:
                    return SupportContextVariableEnum.BuildErrorColumn;
                case UnitTestFramework:
                    return SupportContextVariableEnum.UnitTestFramework;
            }

            return SupportContextVariableEnum.Unknown;
        }

        public static string GetAnchor(
            this SupportContextVariableEnum variable
            )
        {
            switch (variable)
            {
                case SupportContextVariableEnum.ContextItemFilePath:
                    return ContextItemFilePath;
                case SupportContextVariableEnum.BuildErrorMessage:
                    return BuildErrorMessage;
                case SupportContextVariableEnum.BuildErrorLine:
                    return BuildErrorLine;
                case SupportContextVariableEnum.BuildErrorColumn:
                    return BuildErrorColumn;
                case SupportContextVariableEnum.UnitTestFramework:
                    return UnitTestFramework;
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


        public static async Task<SupportContext> WithSelectedFilesAsync(
            List<SolutionItem> selections
            )
        {
            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            var result = new SupportContext();

            result.AddContextVariable(
                SupportContextVariableEnum.ContextItemFilePath,
                string.Join(", ", selections.Select(s => $"`{s.FullPath}`"))
                );
            result.AddContextVariable(
                SupportContextVariableEnum.UnitTestFramework,
                unsorted.PreferredUnitTestFramework
                );

            return result;
        }

        public static async Task<SupportContext> WithContextCodeAsync(
            string filePath
            )
        {
            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            var result = new SupportContext();

            result.AddContextVariable(
                SupportContextVariableEnum.ContextItemFilePath,
                filePath
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
            var unsorted = await FreeAIrOptions.DeserializeUnsortedAsync();

            var result = new SupportContext();

            result.AddContextVariable(
                SupportContextVariableEnum.ContextItemFilePath,
                errorInformation.FilePath
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
