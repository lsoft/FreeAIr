using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeAIr.Chat
{
    public sealed class AddLineNumbersMode
    {
        private readonly AddLineNumbersModeEnum _mode;
        private List<(int StartLine, int LineCount)> _scopes;

        public static readonly AddLineNumbersMode NotRequired = new AddLineNumbersMode(AddLineNumbersModeEnum.Disabled);

        public static readonly AddLineNumbersMode RequiredAllInScope = new AddLineNumbersMode(AddLineNumbersModeEnum.AllInScope);

        public bool Enabled => _mode != AddLineNumbersModeEnum.Disabled;

        private AddLineNumbersMode(
            AddLineNumbersModeEnum mode
            )
        {
            _mode = mode;
            _scopes = new();
        }

        private AddLineNumbersMode(
            List<(int StartLine, int LineCount)> scopes
            )
        {
            _mode = AddLineNumbersModeEnum.SpecificScopes;
            _scopes = scopes;
        }

        public static AddLineNumbersMode RequiredForScopes(
            List<(int StartLine, int LineCount)> scopes
            )
        {
            return new AddLineNumbersMode(scopes);
        }

        public string AddLineNumbers(
            string body,
            string lineEnding
            )
        {
            if (!Enabled)
            {
                return body;
            }

            var lines = body.Split(new[] { lineEnding }, StringSplitOptions.None);

            var digitCount = Math.Ceiling(Math.Log10(lines.Length));
            var stringFormat = "D" + digitCount.ToString();

            var result = new StringBuilder();
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                var line = lines[lineIndex];

                switch (_mode)
                {
                    case AddLineNumbersModeEnum.AllInScope:
                        {
                            result.Append(lineIndex.ToString(stringFormat));
                            result.Append(": ");
                        }
                        break;
                    case AddLineNumbersModeEnum.SpecificScopes:
                        {
                            if (_scopes.Any(s => s.StartLine <= lineIndex && (s.StartLine + s.LineCount) > lineIndex))
                            {
                                result.Append(lineIndex.ToString(stringFormat));
                                result.Append(": ");
                            }
                        }
                        break;
                }

                result.AppendLine(line);
            }

            return result.ToString();
        }
    }

    public enum AddLineNumbersModeEnum
    {
        Disabled,
        AllInScope,
        SpecificScopes
    }

}
