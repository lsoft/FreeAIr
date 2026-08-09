using FreeAIr.Commands.Other;
using FreeAIr.Helper;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell.Interop;

namespace FreeAIr.InfoBar
{
    /// <summary>
    /// Shows the first-run info bar: FreeAIr has never run in this install before, so it offers
    /// the release notes and the setup wizard together, once. Shown instead of
    /// <see cref="ReleaseNotesInfoBarService"/> on the run it applies to - see
    /// <see cref="FreeAIrPackage.ShowSetupWizardInfoBarIfNeeded"/>.
    /// </summary>
    public class SetupWizardInfoBarService : InfoBarService
    {
        private static readonly object _locker = new object();
        private static volatile SetupWizardInfoBarService _instance;

        public static SetupWizardInfoBarService Instance => _instance;

        public static void Initialize(IServiceProvider serviceProvider)
        {
            if (_instance is null)
            {
                lock (_locker)
                {
                    if (_instance is null)
                    {
                        _instance = new SetupWizardInfoBarService(
                            serviceProvider
                            );
                    }
                }
            }
        }

        public SetupWizardInfoBarService(
            IServiceProvider serviceProvider
            )
            : base(serviceProvider)
        {
        }

        /// <summary>
        /// Opens the release notes and the setup wizard together when the user asks for them, or
        /// just records the bar as seen when dismissed. Either way <see cref="InternalPage.SetupWizardIntroduced"/>
        /// and <see cref="InternalPage.FreeAIrLastVersion"/> are set, so this bar shows only once and
        /// <see cref="ReleaseNotesInfoBarService"/> does not take its place on the next start - the
        /// wizard stays reachable from the menu regardless.
        /// </summary>
        public override void OnActionItemClicked(IVsInfoBarUIElement infoBarUIElement, IVsInfoBarActionItem actionItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            //this runs on VS's own info bar callback: an exception escaping here takes the shell
            //down with it, so nothing in this method is allowed to throw
            try
            {
                var choose = (int)actionItem.ActionContext;

                InternalPage.Instance.SetupWizardIntroduced = true;
                //this bar links to the release notes itself, so the running version counts as
                //seen: without this the plain "new version installed" bar would greet the user on
                //the very next start of a brand new install
                InternalPage.Instance.FreeAIrLastVersion = Vsix.Version;
                InternalPage.Instance.Save();

                if (choose == 1)
                {
                    var shell = (IVsUIShell)_serviceProvider.GetService(typeof(SVsUIShell));
                    shell?.PostExecCommand(
                        PackageGuids.FreeAIr,
                        PackageIds.ShowReleaseNotesCommandId,
                        0,
                        null
                        );

                    OpenSetupWizardCommand.ShowAsync(isFirstRun: true)
                        .FileAndForget(nameof(SetupWizardInfoBarService) + "." + nameof(OnActionItemClicked));
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
            finally
            {
                infoBarUIElement.Close();
            }
        }

        /// <summary>
        /// Builds the first-run info bar text with its "Open release notes and the setup wizard" /
        /// "Not interested" links.
        /// </summary>
        protected override InfoBarModel GetModel()
        {
            return new InfoBarModel(
                new InfoBarTextSpan[]
                {
                    new InfoBarTextSpan(Resources.Resources.Wizard_infobar_message)
                },
                new InfoBarActionItem[]
                {
                    new InfoBarHyperlink(Resources.Resources.Wizard_infobar_open, 1),
                    new InfoBarHyperlink(Resources.Resources.Not_interested, 2)
                },
                KnownMonikers.NeuralNetwork,
                isCloseButtonVisible: false
                );
        }
    }
}
