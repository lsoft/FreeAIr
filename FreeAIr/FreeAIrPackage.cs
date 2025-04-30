global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using SauronEye.UI.Informer;
using System.Runtime.InteropServices;
using System.Threading;

namespace FreeAIr
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.FreeAIrString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(OptionsProvider.ApiPageOptions), "FreeAIr", "Api", 0, 0, true, SupportsProfiles = true)]
    [ProvideToolWindow(typeof(TaskListToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    public sealed class FreeAIrPackage : ToolkitPackage
    {
        public static FreeAIrPackage Instance = null;

        public FreeAIrPackage(
            )
        {
            Instance = this;
        }

        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress
            )
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await this.RegisterCommandsAsync();

            this.RegisterToolWindows();

            var componentModel = (IComponentModel)await this.GetServiceAsync(typeof(SComponentModel));
            StartServices(componentModel);
        }


        private static void StartServices(
            IComponentModel componentModel
            )
        {

            var uii = componentModel.GetService<UIInformer>();
            uii.InitAsync()
                .FileAndForget(nameof(UIInformer));

            uii.DoubleClickEvent += UIDoubleClickEvent;
        }

        private static async void UIDoubleClickEvent(object sender, EventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                _ = await TaskListToolWindow.ShowAsync();
            }
            catch (Exception excp)
            {
                //todo
            }
        }
    }
}