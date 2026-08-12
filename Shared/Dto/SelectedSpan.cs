using Microsoft.VisualStudio.Text;

namespace FreeAIr.UI.Embedillo.Answer.Parser
{
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
