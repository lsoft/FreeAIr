global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using FreeAIr.BLogic;
using FreeAIr.UI.ToolWindows;
using MessagePack.Formatters;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell.Interop;
using SauronEye.UI.Informer;
using System.Collections.Generic;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
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
    [ProvideOptionPage(typeof(OptionsProvider.ResponsePageOptions), "FreeAIr", "Response", 0, 0, true, SupportsProfiles = true)]
    [ProvideToolWindow(typeof(ChatListToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(ChooseModelToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    public sealed class FreeAIrPackage : ToolkitPackage
    {
        public static FreeAIrPackage Instance = null;

        public static readonly string WorkingFolder;

        static FreeAIrPackage()
        {
            var eal = Assembly.GetExecutingAssembly().Location;
            var exeFolderPath = new System.IO.FileInfo(eal).Directory.FullName;
            WorkingFolder = exeFolderPath;
        }

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
            //load dlls manually, for unknown reason these dlls does not loaded automatically
            Assembly a1 = Assembly.LoadFrom(System.IO.Path.Combine(WorkingFolder, "MdXaml.dll"));
            AppDomain.CurrentDomain.Load(a1.FullName);

            ResponsePage.LoadOrUpdateMarkdownStyles();

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
            var cmb = componentModel.GetService<CommitMessageBuilder>();
            cmb.RunAsync()
                .FileAndForget(nameof(CommitMessageBuilder));

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

                _ = await ChatListToolWindow.ShowAsync();
            }
            catch (Exception excp)
            {
                //todo
            }
        }
    }
}