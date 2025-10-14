using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;

namespace FreeAIr.UI.InSitu
{
    public sealed class InSituChatInputCommandFilter : IOleCommandTarget
    {
        private static bool _suppress;

        public static bool GetSuppressMode()
        {
            return _suppress;
        }

        public static void SetSuppressMode(bool suppress)
        {
            _suppress = suppress;
        }


        public IOleCommandTarget? Next
        {
            get;
            set;
        }

        public InSituChatInputCommandFilter()
        {
        }


        public int QueryStatus(ref Guid pguidCmdGroup, uint nCmdID, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (_suppress)
            {
                return VSConstants.S_FALSE;
            }

            return
                Next?.QueryStatus(ref pguidCmdGroup, nCmdID, prgCmds, pCmdText)
                ?? VSConstants.S_OK
                ;
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (_suppress)
            {
                return VSConstants.S_FALSE;
            }

            return Next != null
                ? Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut)
                : VSConstants.S_OK
                ;
        }
    }
}
