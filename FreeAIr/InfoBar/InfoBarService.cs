using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace FreeAIr.InfoBar
{
    public abstract class InfoBarService : IVsInfoBarUIEvents
    {

        protected readonly IServiceProvider _serviceProvider;
        private uint _cookie;

        protected InfoBarService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void OnClosed(IVsInfoBarUIElement infoBarUIElement)
        {
            infoBarUIElement.Unadvise(_cookie);
        }

        public abstract void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem);

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

        protected abstract InfoBarModel GetModel();
    }
}
