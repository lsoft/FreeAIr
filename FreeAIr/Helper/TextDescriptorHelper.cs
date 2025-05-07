using Community.VisualStudio.Toolkit;
using FreeAIr.BLogic;
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
    public static class TextDescriptorHelper
    {
        public static async Task<IOriginalTextDescriptor?> GetSelectedTextAsync()
        {
            var docView = await VS.Documents.GetActiveDocumentViewAsync();
            if (docView?.TextView == null)
            {
                //not a text window
                return null;
            }

            var lineEnding = LineEndingHelper.Actual.GetActiveDocumentLineEnding();

            return new SelectedTextDescriptor(
                docView,
                lineEnding
                );
        }
    }
}
