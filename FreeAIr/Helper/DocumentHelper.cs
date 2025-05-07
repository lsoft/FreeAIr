using Community.VisualStudio.Toolkit;
using EnvDTE80;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
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
        public static string OpenDocumentAndGetLineEnding(
            string filePath
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;

                var w = dte.ItemOperations.OpenFile(filePath);
                var openDoc = w.Document;

                var lineEnding = GetDocumentLineEnding(openDoc);
                return lineEnding;
            }
            catch (Exception excp)
            {
                //todo log
            }

            return Environment.NewLine;
        }

        public static string GetOpenedDocumentLineEnding(
            string filePath
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;

                foreach (EnvDTE.Document document in dte.Documents)
                {
                    if (string.Compare(
                        document.FullName,
                        filePath,
                        true) == 0)
                    {
                        var lineEnding = GetDocumentLineEnding(document);
                        return lineEnding;
                    }
                }
            }
            catch (Exception excp)
            {
                //todo log
            }

            return Environment.NewLine;
        }

        public static string GetActiveDocumentLineEnding()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;

                var activeDoc = dte.ActiveDocument;
                if (activeDoc is null || activeDoc.ReadOnly)
                {
                    return Environment.NewLine;
                }

                var lineEnding = GetDocumentLineEnding(activeDoc);
                return lineEnding;
            }
            catch (Exception excp)
            {
                //todo log
            }

            return Environment.NewLine;
        }

        private static string GetDocumentLineEnding(
            EnvDTE.Document activeDoc
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (activeDoc.Object("TextDocument") is EnvDTE.TextDocument textDoc)
            {
                var ep = textDoc.CreateEditPoint();
                ep.EndOfLine();

                var lineEnding = ep.GetText(null);
                return lineEnding;
            }

            return Environment.NewLine;
        }

        public static async Task<IOriginalTextDescriptor?> GetSelectedTextAsync()
        {
            var docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null)
            {
                //not a text window
                return null;
            }

            var lineEnding = DocumentHelper.GetActiveDocumentLineEnding();

            return new SelectedTextDescriptor(
                docView,
                lineEnding
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

        string LineEnding
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

            _filePath = filePath;
            LineEnding = lineEnding;
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

            var selection = documentView?.TextView.Selection;
            var selectedSpan = selection.StreamSelectionSpan.SnapshotSpan;
            _span = selectedSpan.Span;
            var selectedText = selectedSpan.GetText();

            var fileName = new FileInfo(documentView.FilePath).Name;

            documentView.TextView.Closed += TextView_Closed;

            FileName = fileName;
            OriginalText = selectedText;
            _documentView = documentView;
            LineEnding = lineEnding;
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

            //Microsoft.VisualStudio.Text.ITextDocument document = documentView.Document;

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
