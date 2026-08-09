using FreeAIr.Helper;
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

            if (TryAddInfoBar())
            {
                return;
            }

            //devenv started without a solution has no main window info bar host when the package is
            //autoloaded on the NoSolution context, and still none when the shell reports itself
            //initialized - the start window is up and the main window does not exist yet. Waiting
            //for ShellInitializedContext is therefore not enough; keep looking for the host in the
            //background rather than dropping the bar.
            ActivityLogHelper.ActivityLogInformation(
                $"{GetType().Name}: no info bar host yet, waiting for the main window."
                );

            WaitForInfoBarHostAsync()
                .FileAndForget(nameof(InfoBarService))
                ;
        }

        /// <summary>
        /// Retries adding the info bar once a second until the main window exists, giving up after
        /// a minute so a devenv that never shows one does not keep a timer alive forever.
        /// </summary>
        private async Task WaitForInfoBarHostAsync()
        {
            for (var attempt = 0; attempt < 60; attempt++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1));

                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (TryAddInfoBar())
                {
                    return;
                }
            }

            ActivityLogHelper.ActivityLogWarning(
                $"{GetType().Name}: no info bar host appeared within a minute, the bar is lost."
                );
        }

        /// <summary>
        /// Adds the info bar to the main window right now, reporting whether the host was there to
        /// take it.
        /// </summary>
        private bool TryAddInfoBar()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var shell = (IVsShell)_serviceProvider.GetService(typeof(SVsShell));
            if (shell == null)
            {
                return false;
            }

            shell.GetProperty((int)__VSSPROPID7.VSSPROPID_MainWindowInfoBarHost, out var obj);
            var host = obj as IVsInfoBarHost;

            if (host == null)
            {
                return false;
            }

            var infoBarModel = GetModel();

            var factory = (IVsInfoBarUIFactory)_serviceProvider.GetService(typeof(SVsInfoBarUIFactory));
            var element = factory.CreateInfoBar(infoBarModel);
            element.Advise(this, out _cookie);
            host.AddInfoBar(element);

            ActivityLogHelper.ActivityLogInformation(
                $"{GetType().Name}: info bar shown."
                );

            return true;
        }

        /// <summary>
        /// Builds the text, icon and action items shown in this service's info bar.
        /// </summary>
        protected abstract InfoBarModel GetModel();
    }
}
