
using FreeAIr.Helper;
using System.Text.RegularExpressions;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    public sealed class SelectedIdentifier
    {
        private static readonly Regex _mainParseRegex = new Regex(
            @"^([^:]+):(.+)$",
            RegexOptions.Compiled
            );
        private static readonly Regex _positionParseRegex = new Regex(
            @"^(.+)-(.+)$",
            RegexOptions.Compiled
            );

        public string FilePath
        {
            get;
        }

        public SelectedSpan? Selection
        {
            get;
        }

        public SelectedIdentifier(
            string filePath,
            SelectedSpan? selection
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;
            Selection = selection;
        }


        public override string ToString()
        {
            if (Selection is null)
            {
                return FilePath;
            }

            return $"{FilePath}:{Selection.StartPosition}-{Selection.StartPosition + Selection.Length}";
        }

        public static SelectedIdentifier Parse(string solutionItemText)
        {
            var reversed = solutionItemText.ReverseString();

            var mainMatch = _mainParseRegex.Match(reversed);
            if (mainMatch.Success)
            {
                if (mainMatch.Groups.Count == 3)
                {
                    var lines = mainMatch.Groups[1].Value.ReverseString();
                    var filePath = mainMatch.Groups[2].Value.ReverseString();

                    var positionMatch = _positionParseRegex.Match(lines);
                    if (positionMatch.Success)
                    {
                        if (positionMatch.Groups.Count == 3)
                        {
                            var start = int.Parse(positionMatch.Groups[1].Value);
                            var end = int.Parse(positionMatch.Groups[2].Value);

                            return new(filePath, new SelectedSpan(start, end - start));
                        }
                    }
                }
            }

            return new(solutionItemText, null);
        }

    }

    public sealed class SelectedSpan
    {
        public int StartPosition
        {
            get;
        }
        public int Length
        {
            get;
        }

        public SelectedSpan(
            int startPosition,
            int length
            )
        {
            StartPosition = startPosition;
            Length = length;
        }

        public Microsoft.VisualStudio.Text.Span GetVisualStudioSpan() =>
            new Microsoft.VisualStudio.Text.Span(
                StartPosition,
                Length
                );

        public override string ToString()
        {
            return $":{StartPosition}-{StartPosition + Length}";
        }
    }
}
