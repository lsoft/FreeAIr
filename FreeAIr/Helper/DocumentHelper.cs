using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    public static class DocumentHelper
    {
        public static async Task<(string?, string?)> GetSelectedTextAsync()
        {
            var docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null)
            {
                //not a text window
                return (null, null);
            }

            var selection = docView?.TextView.Selection;
            var selectedSpan = selection.StreamSelectionSpan.SnapshotSpan;
            var selectedText = selectedSpan.GetText();

            //if (string.IsNullOrWhiteSpace(selectedText))
            //{
            //    var line = selection.Start.Position.GetContainingLine();
            //    selectedText = line.GetText();
            //}

            var fileName = new FileInfo(docView.FilePath).Name;

            return (fileName, selectedText);
        }
    }
}
