using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;

namespace FreeAIr.InfoBar
{
    public class ReleaseNotesInfoBarService : InfoBarService
    {
        private static readonly object _locker = new object();
        private static volatile ReleaseNotesInfoBarService _instance;

        public static ReleaseNotesInfoBarService Instance => _instance;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            if (_instance is null)
            {
                lock (_locker)
                {
                    if (_instance is null)
                    {
                        _instance = new ReleaseNotesInfoBarService(
                            serviceProvider
                            );
                    }
                }
            }
        }

        public ReleaseNotesInfoBarService(
            IServiceProvider serviceProvider
            )
            : base(serviceProvider)
        {
        }

        public override void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var choose = (int)actionItem.ActionContext;

            InternalPage.Instance.FreeAIrLastVersion = Vsix.Version;
            InternalPage.Instance.Save();

            switch (choose)
            {
                case 1:
                    var shell = (IVsUIShell)_serviceProvider.GetService(typeof(SVsUIShell));
                    shell.PostExecCommand(
                        PackageGuids.FreeAIr,
                        PackageIds.ShowReleaseNotesCommandId,
                        0,
                        null
                        );
                    break;
                default:
                    break;
            }

            infoBarUIElement.Close();
        }


        protected override InfoBarModel GetModel()
        {
            return new InfoBarModel(
                new InfoBarTextSpan[]
                {
                    new InfoBarTextSpan(FreeAIr.Resources.Resources.New_version_of_FreeAIr_has_been_installed)
                },
                new InfoBarActionItem[]
                {
                        new InfoBarHyperlink(FreeAIr.Resources.Resources.Show_release_notes, 1),
                        new InfoBarHyperlink(FreeAIr.Resources.Resources.Not_interested, 2)
                },
                KnownMonikers.NeuralNetwork,
                isCloseButtonVisible: false
                );
        }

    }
}
