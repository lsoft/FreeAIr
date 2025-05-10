using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.VisualStudio.Text;
using SharpCompress.Common;
using System.IO;
using System.Threading;

namespace FreeAIr.BLogic
{
    public interface IOriginalTextDescriptor : IDisposable
    {
        string FilePath
        {
            get;
        }

        string FileName
        {
            get;
        }

        FreeAIr.UI.Embedillo.Answer.Parser.SelectedSpan? SelectedSpan
        {
            get;
        }

        bool IsAbleToManipulate
        {
            get;
        }

        string OriginalText
        {
            get;
        }

        string LineEnding
        {
            get;
        }

        SelectedIdentifier CreateSelectedIdentifier();

        Task ReplaceOriginalTextWithNewAsync(string newText);
    }

    public sealed class WholeFileTextDescriptor : IOriginalTextDescriptor
    {
        public string FilePath
        {
            get;
        }

        public string FileName
        {
            get;
        }

        public FreeAIr.UI.Embedillo.Answer.Parser.SelectedSpan? SelectedSpan => null;

        public bool IsAbleToManipulate => File.Exists(FilePath);

        public string OriginalText
        {
            get;
        }
        public string LineEnding
        {
            get;
        }

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
            LineEnding = lineEnding;
            FileName = new FileInfo(filePath).Name;
            OriginalText = File.ReadAllText(FilePath);
        }


        public void Dispose()
        {
            //nothing to do
        }

        public Task ReplaceOriginalTextWithNewAsync(string newText)
        {
            if (newText is null)
            {
                throw new ArgumentNullException(nameof(newText));
            }

            File.WriteAllText(FilePath, newText);

            return Task.CompletedTask;
        }

        public SelectedIdentifier CreateSelectedIdentifier()
        {
            return new(FilePath, null);
        }
    }

    public sealed class SelectedTextDescriptor : IOriginalTextDescriptor
    {
        private int _disposed = 0;

        private DocumentView? _documentView;

        public string FilePath
        {
            get;
        }

        public string FileName
        {
            get;
        }

        public FreeAIr.UI.Embedillo.Answer.Parser.SelectedSpan? SelectedSpan
        {
            get;
        }

        public string OriginalText
        {
            get;
        }

        public bool IsAbleToManipulate => _documentView is not null;

        public string LineEnding
        {
            get;
        }

        public SelectedTextDescriptor(
            DocumentView documentView,
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

            var selection = documentView?.TextView.Selection;
            var selectedSpan = selection.StreamSelectionSpan.SnapshotSpan;
            SelectedSpan = new UI.Embedillo.Answer.Parser.SelectedSpan(
                selectedSpan.Span.Start,
                selectedSpan.Span.Length
                );
            var selectedText = selectedSpan.GetText();

            var fileName = new FileInfo(filePath).Name;

            documentView.TextView.Closed += TextView_Closed;

            FilePath = filePath;
            FileName = fileName;
            OriginalText = selectedText;
            _documentView = documentView;
            LineEnding = lineEnding;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            _documentView.TextView.Closed -= TextView_Closed;

            _documentView = null;
        }

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
                    "Cannot edit the document. Please copy-paste the text manually."
                    );
                return;
            }

            if (SelectedSpan is null)
            {
                await VS.MessageBox.ShowErrorAsync(
                    FreeAIr.Resources.Resources.Error,
                    "Cannot edit the document. Please copy-paste the text manually."
                    );
                return;
            }

            using var documentEdit = documentView.TextBuffer.CreateEdit();
            if (documentEdit.Replace(
                new Span(
                    SelectedSpan.StartPosition,
                    SelectedSpan.Length
                    ),
                newText
                ))
            {
                documentEdit.Apply();
            }
        }

        public SelectedIdentifier CreateSelectedIdentifier()
        {
            return new(FilePath, SelectedSpan);
        }

        private void TextView_Closed(object sender, EventArgs e)
        {
            Dispose();
        }

    }
}
