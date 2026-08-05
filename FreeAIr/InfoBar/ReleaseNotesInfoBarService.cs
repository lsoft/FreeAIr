using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;

namespace FreeAIr.InfoBar
{
    /// <summary>
    /// Shows the "a new version of FreeAIr has been installed" info bar with a link to the
    /// release notes, displayed once after an update when the installed version has changed.
    /// </summary>
    public class ReleaseNotesInfoBarService : InfoBarService
    {
        /// <summary>Guards lazy creation of the singleton <see cref="Instance"/>.</summary>
        private static readonly object _locker = new object();
        /// <summary>Backing field for the process-wide singleton instance.</summary>
        private static volatile ReleaseNotesInfoBarService _instance;

        /// <summary>The singleton instance created by <see cref="Initialize"/>.</summary>
        public static ReleaseNotesInfoBarService Instance => _instance;

        /// <summary>
        /// Creates the singleton instance on first call; subsequent calls are no-ops.
        /// </summary>
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

        /// <summary>
        /// Creates the release-notes info bar service bound to the given VS service provider.
        /// </summary>
        public ReleaseNotesInfoBarService(
            IServiceProvider serviceProvider
            )
            : base(serviceProvider)
        {
        }

        /// <summary>
        /// Handles the user's choice on the info bar: opens the "Show release notes" command when
        /// clicked, or just records the current version as seen when dismissed as "not interested".
        /// </summary>
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


        /// <summary>
        /// Builds the "new version installed" info bar text with the neural-network icon and its
        /// "Show release notes" / "Not interested" links.
        /// </summary>
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
