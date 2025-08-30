using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel.Composition;

namespace FreeAIr.UI.InSitu
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    internal sealed class InSituChatInputFilterProvider : IVsTextViewCreationListener
    {
        [Import] internal IVsEditorAdaptersFactoryService Adapters = null!;

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var wpfTextView = Adapters.GetWpfTextView(textViewAdapter);
            var filter = new InSituChatInputCommandFilter();
            textViewAdapter.AddCommandFilter(filter, out var next);
            filter.Next = next;
        }
    }
}
