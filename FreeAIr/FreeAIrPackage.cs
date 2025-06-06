global using Community.VisualStudio.Toolkit;
global using Microsoft.VisualStudio.Shell;
global using System;
global using Task = System.Threading.Tasks.Task;
using EnvDTE80;
using FreeAIr.BLogic;
using FreeAIr.Extension.CodeLens;
using FreeAIr.Find;
using FreeAIr.Helper;
using FreeAIr.InfoBar;
using FreeAIr.MCP.Agent;
using FreeAIr.UI.Informer;
using FreeAIr.UI.ToolWindows;
using FreeAIr.UI.ViewModels;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
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
    [ProvideOptionPage(typeof(OptionsProvider.MCPPageOptions), "FreeAIr", "MCP", 0, 0, true, SupportsProfiles = true)]
    [ProvideOptionPage(typeof(OptionsProvider.InternalPageOptions), "FreeAIr", "Internal", 0, 0, true, SupportsProfiles = true)]
    [ProvideToolWindow(typeof(ChatListToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(ChooseModelToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(NaturalLanguageResultsToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideService(typeof(SolutionItemsContextMenuCommandBridge), IsAsyncQueryable = true)]
    [ProvideService(typeof(FindScopeContextMenuCommandBridge), IsAsyncQueryable = true)]
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
            try
            {
                //load dlls manually, for unknown reason these dlls does not loaded automatically
                //Assembly a1 = Assembly.LoadFrom(System.IO.Path.Combine(WorkingFolder, "MdXaml.dll"));
                //AppDomain.CurrentDomain.Load(a1.FullName);

                Assembly a2 = Assembly.LoadFrom(System.IO.Path.Combine(WorkingFolder, "System.ClientModel.dll"));
                AppDomain.CurrentDomain.Load(a2.FullName);

                Assembly a3 = Assembly.LoadFrom(System.IO.Path.Combine(WorkingFolder, "JsonPath.Net.dll"));
                AppDomain.CurrentDomain.Load(a3.FullName);

                AddService(
                    typeof(SolutionItemsContextMenuCommandBridge),
                    (_, _, _) => Task.FromResult<object>(new SolutionItemsContextMenuCommandBridge()),
                    true
                    );
                AddService(
                    typeof(FindScopeContextMenuCommandBridge),
                    (_, _, _) => Task.FromResult<object>(new FindScopeContextMenuCommandBridge()),
                    true
                    );

                await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                await this.RegisterCommandsAsync();

                this.RegisterToolWindows();

                //refresh codelenses
                //we do not wait it for its completion.
                CodeLensConnectionHandler.AcceptCodeLensConnectionsAsync()
                    .FileAndForget(nameof(CodeLensConnectionHandler.AcceptCodeLensConnectionsAsync))
                    ;

                FindWindowModifier.StartScanAsync(cancellationToken)
                    .FileAndForget(nameof(FindWindowModifier))
                    ;

                var componentModel = (IComponentModel)await this.GetServiceAsync(typeof(SComponentModel));
                StartServices(componentModel);

                ShowReleaseNotesInfoBarIfNeeded();

                EmbeddedResourceHelper.LoadXamlEmbeddedResource("FreeAIr.UI.ClickableText.ClickableTextResource.xaml");

                AgentApplication.UpdateExternalServersAsync()
                    .FileAndForget(nameof(AgentApplication.UpdateExternalServersAsync))
                    ;
            }
            catch (Exception excp)
            {
                //todo log
                throw;
            }
        }

        private static void ShowReleaseNotesInfoBarIfNeeded()
        {
            if (Vsix.Version != InternalPage.Instance.FreeAIrLastVersion)
            {
                var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
                var sp = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte);
                ReleaseNotesInfoBarService.Initialize(sp);
                ReleaseNotesInfoBarService.Instance.ShowInfoBar();
            }
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