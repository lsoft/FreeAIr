using Community.VisualStudio.Toolkit;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class DocumentHelper
    {
        public static async Task<IOriginalTextDescriptor?> GetSelectedTextAsync()
        {
            var docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null)
            {
                //not a text window
                return null;
            }

            return new SelectedTextDescriptor(
                docView
                );
        }
    }

    public interface IOriginalTextDescriptor : IDisposable
    {
        string FileName
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

        Task ReplaceOriginalTextWithNewAsync(string newText);
    }

    public sealed class WholeFileTextDescriptor : IOriginalTextDescriptor
    {
        private readonly string _filePath;

        public string FileName
        {
            get;
        }

        public bool IsAbleToManipulate => File.Exists(_filePath);

        public string OriginalText
        {
            get;
        }

        public WholeFileTextDescriptor(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            _filePath = filePath;
            FileName = new FileInfo(filePath).Name;
            OriginalText = File.ReadAllText(_filePath);
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

            File.WriteAllText(_filePath, newText);

            return Task.CompletedTask;
        }
    }

    public sealed class SelectedTextDescriptor : IOriginalTextDescriptor
    {
        private int _disposed = 0;

        private DocumentView? _documentView;
        private Span _span;

        public string FileName
        {
            get;
        }

        public string OriginalText
        {
            get;
        }

        public bool IsAbleToManipulate => _documentView is not null;

        public SelectedTextDescriptor(
            DocumentView documentView
            )
        {
            if (documentView is null)
            {
                throw new ArgumentNullException(nameof(documentView));
            }

            var selection = documentView?.TextView.Selection;
            var selectedSpan = selection.StreamSelectionSpan.SnapshotSpan;
            _span = selectedSpan.Span;
            var selectedText = selectedSpan.GetText();

            var fileName = new FileInfo(documentView.FilePath).Name;

            documentView.TextView.Closed += TextView_Closed;

            FileName = fileName;
            OriginalText = selectedText;
            _documentView = documentView;
        }

        private void TextView_Closed(object sender, EventArgs e)
        {
            Dispose();
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

            Microsoft.VisualStudio.Text.ITextDocument document = documentView.Document;

            using var documentEdit = documentView.TextBuffer.CreateEdit();
            if (documentEdit.Replace(
                _span,
                newText
                ))
            {
                documentEdit.Apply();
                //document.Save();
            }
        }
    }
}
