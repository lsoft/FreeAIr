
using FreeAIr.Helper;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
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

        public async Task OpenInNewWindowAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                var documentView = await VS.Documents.OpenAsync(FilePath);
                if (documentView is null)
                {
                    return;
                }

                if (Selection is not null)
                {
                    var textView = documentView.TextView;

                    textView.Selection.Select(
                        Selection.GetSnapshotSpan(textView.TextSnapshot),
                        false
                        );
                }
            }
            catch (Exception excp)
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    "Error: "
                    + Environment.NewLine
                    + excp.Message
                    + Environment.NewLine
                    + Environment.NewLine
                    + excp.StackTrace
                    );

                //todo log
            }
        }

        public override string ToString()
        {
            if (Selection is null)
            {
                return FilePath;
            }

            return $"{FilePath}:{Selection.StartPosition}-{Selection.StartPosition + Selection.Length}";
        }

        #region equality

        public override bool Equals(object obj)
        {
            return 
                obj is SelectedIdentifier identifier
                && FilePath == identifier.FilePath
                && (ReferenceEquals(Selection, identifier.Selection) || Selection.Equals(identifier.Selection))
                ;
        }

        public override int GetHashCode()
        {
            var hashCode = 121304889;
            hashCode = hashCode * -1521134295 + FilePath.GetHashCode();
            hashCode = hashCode * -1521134295 + (Selection?.GetHashCode() ?? 0);
            return hashCode;
        }

        #endregion

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

        public SnapshotSpan GetSnapshotSpan(
            ITextSnapshot textSnapshot
            )
        {
            return new SnapshotSpan(textSnapshot, StartPosition, Length);
        }

        #region equality

        public override bool Equals(object obj)
        {
            return
                obj is SelectedSpan span
                && StartPosition == span.StartPosition
                && Length == span.Length
                ;
        }

        public override int GetHashCode()
        {
            var hashCode = -789397647;
            hashCode = hashCode * -1521134295 + StartPosition;
            hashCode = hashCode * -1521134295 + Length;
            return hashCode;
        }

        #endregion
    }
}
