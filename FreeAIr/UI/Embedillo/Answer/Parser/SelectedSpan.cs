
using FreeAIr.Helper;
using Microsoft.VisualStudio.Text;
using System.Text.RegularExpressions;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
    /// <summary>
    /// Identifies a file, optionally combined with a selected text span within it, as
    /// referenced from an Embedillo chat mention (e.g. an @-mentioned file or code
    /// selection). Parses and formats the compact "path:start-end" text representation
    /// used in the chat input, and can open the referenced location in the editor.
    /// </summary>
    public sealed class SelectedIdentifier
    {
        /// <summary>Splits the reversed "path:start-end" text into its file-path and position portions.</summary>
        private static readonly Regex _mainParseRegex = new Regex(
            @"^([^:]+):(.+)$",
            RegexOptions.Compiled
            );
        /// <summary>Splits the reversed position portion of a parsed reference into its start and end offsets.</summary>
        private static readonly Regex _positionParseRegex = new Regex(
            @"^(.+)-(.+)$",
            RegexOptions.Compiled
            );

        /// <summary>The current solution's path at creation time, used to render <see cref="ContextUIDescription"/> relative to it.</summary>
        private readonly string? _solutionFilePath;

        /// <summary>
        /// Absolute path of the file this identifier points to.
        /// </summary>
        public string FilePath
        {
            get;
        }

        /// <summary>
        /// The selected text range within the file, or null when the whole file is referenced.
        /// </summary>
        public SelectedSpan? Selection
        {
            get;
        }

        /// <summary>
        /// Human readable text shown in the Embedillo UI for this reference: the file
        /// path made relative to the current solution, plus the selection span if any.
        /// </summary>
        public string ContextUIDescription
        {
            get
            {
                var relative =
                    _solutionFilePath is not null
                    ? FilePath.MakeRelativeAgainst(_solutionFilePath)
                    : FilePath
                    ;

                return relative + Selection?.ToString();
            }
        }

        /// <summary>
        /// Builds an identifier for the given file and optional selection, capturing the
        /// current solution's path so <see cref="ContextUIDescription"/> can show a relative path.
        /// </summary>
        public static SelectedIdentifier Create(
            string filePath,
            SelectedSpan? selection
            )
        {
            var solution = VS.Solutions.GetCurrentSolution();
            var solutionFilePath = solution?.FullPath;
            return new SelectedIdentifier(
                solutionFilePath,
                filePath,
                selection
                );
        }

        /// <summary>Stores the solution path, file path and optional selection backing this identifier; use <see cref="Create"/> instead.</summary>
        private SelectedIdentifier(
            string? solutionFilePath,
            string filePath,
            SelectedSpan? selection
            )
        {
            

            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _solutionFilePath = solutionFilePath;
            FilePath = filePath.Trim();
            Selection = selection;
        }

        /// <summary>
        /// Opens the referenced file in a new editor window and, if a selection is
        /// present, highlights that span in the text view.
        /// </summary>
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

                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Formats this identifier back into the compact "path:start-end" text form
        /// used in the chat input, matching what <see cref="Parse"/> expects.
        /// </summary>
        public override string ToString()
        {
            if (Selection is null)
            {
                return FilePath;
            }

            return $"{FilePath}:{Selection.StartPosition}-{Selection.StartPosition + Selection.Length}";
        }

        #region equality

        /// <summary>Two identifiers are equal when they point at the same file path and the same selection.</summary>
        public override bool Equals(object obj)
        {
            return
                obj is SelectedIdentifier identifier
                && FilePath == identifier.FilePath
                && (ReferenceEquals(Selection, identifier.Selection) || Selection.Equals(identifier.Selection))
                ;
        }

        /// <summary>Combines the file path and selection hashes so equal identifiers hash the same.</summary>
        public override int GetHashCode()
        {
            var hashCode = 121304889;
            hashCode = hashCode * -1521134295 + FilePath.GetHashCode();
            hashCode = hashCode * -1521134295 + (Selection?.GetHashCode() ?? 0);
            return hashCode;
        }

        #endregion

        /// <summary>
        /// Parses the compact "path:start-end" text representation used in Embedillo
        /// chat mentions back into a <see cref="SelectedIdentifier"/>, falling back to
        /// treating the whole text as a bare file path when it does not match that shape.
        /// </summary>
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

                            return SelectedIdentifier.Create(filePath, new SelectedSpan(start, end - start));
                        }
                    }
                }
            }

            return SelectedIdentifier.Create(solutionItemText, null);
        }
    }

    /// <summary>
    /// A text range within a file, expressed as a start offset and length, used to
    /// carry an editor selection through Embedillo chat mentions and back into the
    /// Visual Studio text APIs.
    /// </summary>
    public sealed class SelectedSpan
    {
        /// <summary>
        /// Zero-based character offset where the selection begins.
        /// </summary>
        public int StartPosition
        {
            get;
        }
        /// <summary>
        /// Number of characters covered by the selection.
        /// </summary>
        public int Length
        {
            get;
        }

        /// <summary>
        /// Creates a span from a start offset and length.
        /// </summary>
        public SelectedSpan(
            int startPosition,
            int length
            )
        {
            StartPosition = startPosition;
            Length = length;
        }

        /// <summary>
        /// Converts this span to the Visual Studio editor's own <see cref="Microsoft.VisualStudio.Text.Span"/> type.
        /// </summary>
        public Microsoft.VisualStudio.Text.Span GetVisualStudioSpan() =>
            new Microsoft.VisualStudio.Text.Span(
                StartPosition,
                Length
                );

        /// <summary>
        /// Formats this span as the ":start-end" suffix used when rendering a
        /// <see cref="SelectedIdentifier"/> back to text.
        /// </summary>
        public override string ToString()
        {
            return $":{StartPosition}-{StartPosition + Length}";
        }

        /// <summary>
        /// Resolves this span against a live text snapshot so it can be applied as an
        /// editor selection.
        /// </summary>
        public SnapshotSpan GetSnapshotSpan(
            ITextSnapshot textSnapshot
            )
        {
            return new SnapshotSpan(textSnapshot, StartPosition, Length);
        }

        #region equality

        /// <summary>Two spans are equal when they share the same start offset and length.</summary>
        public override bool Equals(object obj)
        {
            return
                obj is SelectedSpan span
                && StartPosition == span.StartPosition
                && Length == span.Length
                ;
        }

        /// <summary>Combines the start offset and length so equal spans hash the same.</summary>
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
