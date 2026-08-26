using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeAIr.Chat
{
    /// <summary>
    /// Whether the source code of a chat context item is prefixed with line numbers before it is
    /// sent to the model, and which lines get them.
    ///
    /// Line numbers are what lets an answer point at a place in the file — "replace lines 40 to 52"
    /// — instead of quoting the code back. They are not free: every number is extra tokens, and a
    /// model reading numbered code sometimes echoes the numbers into the code it writes. So the
    /// decision is made per context item by whoever builds the prompt, not globally.
    ///
    /// Instances are created through the three factories below; the constructors are private so
    /// that a mode is always one of the three shapes the formatter knows.
    /// </summary>
    public sealed class AddLineNumbersMode
    {
        /// <summary>Which of the three numbering shapes this instance represents.</summary>
        private readonly AddLineNumbersModeEnum _mode;

        /// <summary>
        /// The line ranges to number, used by SpecificScopes only. Half open: a scope covers
        /// StartLine up to but not including StartLine plus LineCount.
        /// </summary>
        private List<(int StartLine, int LineCount)> _scopes;

        /// <summary>Send the code as it is. The default for anything the user did not select.</summary>
        public static readonly AddLineNumbersMode NotRequired = new AddLineNumbersMode(AddLineNumbersModeEnum.Disabled);

        /// <summary>Number every line of the fragment that goes into the prompt.</summary>
        public static readonly AddLineNumbersMode RequiredAllInScope = new AddLineNumbersMode(AddLineNumbersModeEnum.AllInScope);

        /// <summary>Which of the three numbering shapes this instance represents, for persistence.</summary>
        public AddLineNumbersModeEnum Mode => _mode;

        /// <summary>The numbered ranges when this is <see cref="AddLineNumbersModeEnum.SpecificScopes"/>.</summary>
        public IReadOnlyList<(int StartLine, int LineCount)> Scopes => _scopes;

        /// <summary>True unless this is the <see cref="NotRequired"/> (disabled) mode.</summary>
        public bool Enabled => _mode != AddLineNumbersModeEnum.Disabled;

        /// <summary>Creates the <see cref="NotRequired"/> or <see cref="RequiredAllInScope"/> singleton shapes.</summary>
        private AddLineNumbersMode(
            AddLineNumbersModeEnum mode
            )
        {
            _mode = mode;
            _scopes = new();
        }

        /// <summary>Creates the <see cref="AddLineNumbersModeEnum.SpecificScopes"/> shape for the given ranges.</summary>
        private AddLineNumbersMode(
            List<(int StartLine, int LineCount)> scopes
            )
        {
            _mode = AddLineNumbersModeEnum.SpecificScopes;
            _scopes = scopes;
        }

        /// <summary>
        /// Number only the given line ranges. Used when the interesting part of a file is known —
        /// the members a code lens or a build error points at — and the rest is sent as plain
        /// context the model should read but not address.
        /// </summary>
        public static AddLineNumbersMode RequiredForScopes(
            List<(int StartLine, int LineCount)> scopes
            )
        {
            return new AddLineNumbersMode(scopes);
        }

        /// <summary>
        /// Prefixes the lines of a file body with their zero based index, in the form the prompts
        /// expect: `NNN: ` followed by the original line.
        ///
        /// The width is the same for every line of one body, so the code stays aligned and reads as
        /// code rather than as a ragged list.
        /// </summary>
        public string AddLineNumbers(
            string body,
            string lineEnding
            )
        {
            if (!Enabled)
            {
                return body;
            }

            //split on the line ending of this very document rather than on Environment.NewLine:
            //getting it wrong merges the whole file into one numbered line
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

    /// <summary>The three shapes of <see cref="AddLineNumbersMode"/>.</summary>
    public enum AddLineNumbersModeEnum
    {
        /// <summary>The body goes into the prompt untouched.</summary>
        Disabled,

        /// <summary>Every line of the body is numbered.</summary>
        AllInScope,

        /// <summary>Only the lines inside the requested ranges are numbered.</summary>
        SpecificScopes
    }

}
