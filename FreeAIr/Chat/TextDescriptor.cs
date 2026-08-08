using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.VisualStudio.Text;
using System.IO;
using System.Threading;

namespace FreeAIr.Chat
{
    /// <summary>
    /// The piece of source an answer is about and can be written back into: either a whole file or
    /// the selection in an open document.
    ///
    /// It exists so that "apply the answer" is one operation whatever the user started from. A chat
    /// begun on a file rewrites the file; a chat begun on a selection replaces exactly the selected
    /// span and leaves the rest of the document alone.
    ///
    /// Disposable because the selected variant subscribes to the editor: a descriptor whose document
    /// has been closed can no longer write anything, and says so through
    /// <see cref="IsAbleToManipulate"/>.
    /// </summary>
    public interface IOriginalTextDescriptor : IDisposable
    {
        /// <summary>Full path of the file the text comes from.</summary>
        string FilePath
        {
            get;
        }

        /// <summary>File name alone — what the chat is titled with in the tool window.</summary>
        string FileName
        {
            get;
        }

        /// <summary>
        /// The selected range, or null when the descriptor stands for the whole file. Also the flag
        /// which tells the two cases apart.
        /// </summary>
        FreeAIr.UI.Embedillo.Answer.Parser.SelectedSpan? SelectedSpan
        {
            get;
        }

        /// <summary>
        /// Whether the text can still be written back. False once the file is gone or the document
        /// has been closed, and the reason the apply button is disabled instead of failing.
        /// </summary>
        bool IsAbleToManipulate
        {
            get;
        }

        /// <summary>
        /// Describes the same place as a <see cref="SelectedIdentifier"/>, the form the chat context
        /// and the `#name` completions of the prompt box work in.
        /// </summary>
        SelectedIdentifier CreateSelectedIdentifier();

        /// <summary>
        /// Writes the model's text over the original. What gets replaced is the whole file or just
        /// the selection, depending on the implementation.
        /// </summary>
        Task ReplaceOriginalTextWithNewAsync(string newText);
    }

    /// <summary>
    /// A whole file on disk, opened in an editor or not.
    ///
    /// Holds no editor state, so it stays usable for as long as the file exists — this is what
    /// chats started from the solution tree, from the build error list or from an MCP tool use.
    /// </summary>
    public sealed class WholeFileTextDescriptor : IOriginalTextDescriptor
    {
        /// <summary>
        /// The line ending of the original, captured at construction. The model answers in whatever
        /// it likes, and writing that back verbatim would rewrite every line of the file.
        /// </summary>
        private readonly string _lineEnding;

        /// <summary>Full path of the file this descriptor writes back to.</summary>
        public string FilePath
        {
            get;
        }

        /// <summary>File name alone, used to title the chat in the tool window.</summary>
        public string FileName
        {
            get;
        }

        /// <summary>Always null: the descriptor is the whole file, so there is no span to narrow it to.</summary>
        public FreeAIr.UI.Embedillo.Answer.Parser.SelectedSpan? SelectedSpan => null;

        /// <summary>Checked against the disk on every read — the file may have been deleted since.</summary>
        public bool IsAbleToManipulate => File.Exists(FilePath);

        public WholeFileTextDescriptor(
            string filePath,
            string lineEnding
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (lineEnding is null)
            {
                throw new ArgumentNullException(nameof(lineEnding));
            }

            FilePath = filePath;
            _lineEnding = lineEnding;
            FileName = new FileInfo(filePath).Name;
        }


        /// <summary>Nothing is held, so nothing is released. Present only to satisfy the interface.</summary>
        public void Dispose()
        {
            //nothing to do
        }

        /// <summary>
        /// Overwrites the file, restoring the original line endings first. Goes straight to disk
        /// rather than through the editor, so an open document will show the change as an external
        /// modification.
        /// </summary>
        public Task ReplaceOriginalTextWithNewAsync(string newText)
        {
            if (newText is null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            File.WriteAllText(
                FilePath,
                newText.WithLineEnding(_lineEnding)
                );

            return Task.CompletedTask;
        }

        /// <summary>Identifies the file with no span, which is how the context items denote a whole file.</summary>
        public SelectedIdentifier CreateSelectedIdentifier()
        {
            return SelectedIdentifier.Create(FilePath, null);
        }
    }

    /// <summary>
    /// A range selected in an open document.
    ///
    /// Tied to the editor for its whole life: it writes through the text buffer so the change lands
    /// in the undo stack and the user can take it back with Ctrl+Z, and it lets go of the view the
    /// moment the document is closed. From then on it refuses to write rather than resurrecting a
    /// tab behind the user's back.
    /// </summary>
    public sealed class SelectedTextDescriptor : IOriginalTextDescriptor
    {
        private readonly string _lineEnding;

        /// <summary>Makes disposal idempotent — it arrives both from the close event and from the owner.</summary>
        private int _disposed = 0;

        /// <summary>The open document, or null once it has been closed. See <see cref="IsAbleToManipulate"/>.</summary>
        private DocumentView? _documentView;

        /// <summary>Full path of the document the selected span belongs to.</summary>
        public string FilePath
        {
            get;
        }

        /// <summary>File name alone, used to title the chat in the tool window.</summary>
        public string FileName
        {
            get;
        }

        /// <summary>The range that was selected when the chat was started, in the coordinates of that moment.</summary>
        public FreeAIr.UI.Embedillo.Answer.Parser.SelectedSpan? SelectedSpan
        {
            get;
        }

        /// <summary>False once the document has been closed: there is no buffer left to edit.</summary>
        public bool IsAbleToManipulate => _documentView is not null;

        /// <summary>
        /// Captures the view, the span and the line ending, and subscribes to the closing of the
        /// text view so the descriptor releases it instead of keeping a dead editor alive.
        /// </summary>
        public SelectedTextDescriptor(
            DocumentView documentView,
            SelectedSpan? selectedSpan,
            string lineEnding
            )
        {
            if (documentView is null)
            {
                throw new ArgumentNullException(nameof(documentView));
            }

            if (lineEnding is null)
            {
                throw new ArgumentNullException(nameof(lineEnding));
            }

            var filePath = documentView.FilePath;

            var fileName = new FileInfo(filePath).Name;

            documentView.TextView.Closed += TextView_Closed;

            FilePath = filePath;
            FileName = fileName;
            _documentView = documentView;
            SelectedSpan = selectedSpan;
            _lineEnding = lineEnding;
        }

        /// <summary>
        /// Unsubscribes from the text view and forgets it. Interlocked because the close event and
        /// the owning chat both call it, and unsubscribing twice from a torn down view throws.
        /// </summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _documentView.TextView.Closed -= TextView_Closed;

            _documentView = null;
        }

        /// <summary>
        /// Replaces the selected span through the text buffer, which puts the change into the undo
        /// stack and leaves the file dirty rather than saved.
        ///
        /// A closed document or a missing span is reported to the user and abandoned: by the time
        /// the answer arrives the tab may well be gone, and there is nowhere left to put the text.
        /// </summary>
        public async Task ReplaceOriginalTextWithNewAsync(
            string newText
            )
        {
            if (newText is null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            var documentView = _documentView;
            if (documentView is null)
            {
                await VS.MessageBox.ShowErrorAsync(
                    Resources.Resources.Error,
                    FreeAIr.Resources.Resources.Cannot_edit_the_document__Please
                    );
                return;
            }

            if (SelectedSpan is null)
            {
                await VS.MessageBox.ShowErrorAsync(
                    FreeAIr.Resources.Resources.Error,
                    FreeAIr.Resources.Resources.Cannot_edit_the_document__Please
                    );
                return;
            }

            using var documentEdit = documentView.TextBuffer.CreateEdit();
            if (documentEdit.Replace(
                new Span(
                    SelectedSpan.StartPosition,
                    SelectedSpan.Length
                    ),
                newText.WithLineEnding(_lineEnding)
                ))
            {
                documentEdit.Apply();
            }
        }

        /// <summary>
        /// Identifies the file together with its span, so the context item carries only the selected
        /// lines into the prompt instead of the whole document.
        /// </summary>
        public SelectedIdentifier CreateSelectedIdentifier()
        {
            return SelectedIdentifier.Create(FilePath, SelectedSpan);
        }

        /// <summary>The document has been closed — release it, and with it the ability to write back.</summary>
        private void TextView_Closed(object sender, EventArgs e)
        {
            Dispose();
        }

    }
}
