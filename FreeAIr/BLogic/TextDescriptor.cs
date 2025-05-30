using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.Answer.Parser;
using Microsoft.VisualStudio.Text;
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

        SelectedIdentifier CreateSelectedIdentifier();

        Task ReplaceOriginalTextWithNewAsync(string newText);
    }

    public sealed class WholeFileTextDescriptor : IOriginalTextDescriptor
    {
        private readonly string _lineEnding;

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

            File.WriteAllText(
                FilePath,
                newText.WithLineEnding(_lineEnding)
                );

            return Task.CompletedTask;
        }

        public SelectedIdentifier CreateSelectedIdentifier()
        {
            return new(FilePath, null);
        }
    }

    public sealed class SelectedTextDescriptor : IOriginalTextDescriptor
    {
        private readonly string _lineEnding;

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

        public bool IsAbleToManipulate => _documentView is not null;

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

            var fileName = new FileInfo(filePath).Name;

            documentView.TextView.Closed += TextView_Closed;

            FilePath = filePath;
            FileName = fileName;
            _documentView = documentView;
            _lineEnding = lineEnding;
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
                newText.WithLineEnding(_lineEnding)
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
