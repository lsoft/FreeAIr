using Microsoft.VisualStudio.Shell.Interop;

namespace FreeAIr.InfoBar
{
    /// <summary>
    /// Base class for showing a Visual Studio main-window info bar (the yellow notification strip
    /// at the top of the IDE) and reacting to its buttons being clicked, such as the FreeAIr
    /// "new version installed" notice.
    /// </summary>
    public abstract class InfoBarService : IVsInfoBarUIEvents
    {

        /// <summary>Service provider used to reach the VS shell's info bar host and factory.</summary>
        protected readonly IServiceProvider _serviceProvider;
        /// <summary>Advise cookie for unsubscribing from info bar UI events when it closes.</summary>
        private uint _cookie;

        /// <summary>
        /// Creates the service bound to the given VS service provider.
        /// </summary>
        protected InfoBarService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Unsubscribes from the info bar's UI events once it has been closed.
        /// </summary>
        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            infoBarUIElement.Unadvise(_cookie);
        }

        /// <summary>
        /// Handles the user clicking one of the info bar's action items (link or button).
        /// </summary>
        public abstract void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem);

        /// <summary>
        /// Builds this service's info bar model and adds it to the main window's info bar host,
        /// making it visible to the user.
        /// </summary>
        public void ShowInfoBar()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var shell = (IVsShell)_serviceProvider.GetService(typeof(SVsShell));
            if (shell != null)
            {
                shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
                var host = (IVsInfoBarHost)obj;

                if (host == null)
                {
                    return;
                }

                var infoBarModel = GetModel();

                var factory = (IVsInfoBarUIFactory)_serviceProvider.GetService(typeof(SVsInfoBarUIFactory));
                var element = factory.CreateInfoBar(infoBarModel);
                element.Advise(this, out _cookie);
                host.AddInfoBar(element);
            }
        }

        /// <summary>
        /// Builds the text, icon and action items shown in this service's info bar.
        /// </summary>
        protected abstract InfoBarModel GetModel();
    }
}
