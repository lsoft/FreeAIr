using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;

namespace FreeAIr.UI.InSitu
{
    /// <summary>
    /// OLE command filter chained onto the in-situ (inline, in-editor) chat input's text view, letting
    /// FreeAIr temporarily suppress all editor commands while the in-situ chat popup owns keyboard input.
    /// </summary>
    public sealed class InSituChatInputCommandFilter : IOleCommandTarget
    {
        /// <summary>When true, this filter swallows every command instead of forwarding it to <see cref="Next"/>.</summary>
        private static bool _suppress;

        /// <summary>Returns whether command suppression is currently active.</summary>
        public static bool GetSuppressMode()
        {
            return _suppress;
        }

        /// <summary>Turns command suppression on or off for the in-situ chat input.</summary>
        public static void SetSuppressMode(bool suppress)
        {
            _suppress = suppress;
        }


        /// <summary>The next command target in the chain, invoked when suppression is off.</summary>
        public IOleCommandTarget? Next
        {
            get;
            set;
        }

        /// <summary>Creates the filter with no downstream target; set <see cref="Next"/> once chained into the editor's command pipeline.</summary>
        public InSituChatInputCommandFilter()
        {
        }


        /// <summary>Reports command status, short-circuiting with S_FALSE while suppression is active.</summary>
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

        /// <summary>Executes the command, short-circuiting with S_FALSE while suppression is active so it never reaches the editor.</summary>
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
