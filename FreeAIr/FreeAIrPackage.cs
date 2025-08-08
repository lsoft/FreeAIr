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
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.UI;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Informer;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

//[assembly: ProvideCodeBase(AssemblyName = "Xceed.Wpf.Toolkit")]
//[assembly: ProvideCodeBase(AssemblyName = "System.ClientModel")]
//[assembly: ProvideCodeBase(AssemblyName = "JsonPath.Net")]

namespace FreeAIr
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.FreeAIrString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(OptionsProvider.InternalPageOptions), "FreeAIr", "Internal", 0, 0, true, SupportsProfiles = true)]
    [ProvideOptionPage(typeof(OptionsProvider.FontSizePageOptions), "FreeAIr", "Font sizes", 0, 0, true, SupportsProfiles = true)]
    [ProvideToolWindow(typeof(ChatListToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(ChooseModelToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(NaturalLanguageResultsToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(NaturalLanguageOutlinesToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(BuildNaturalLanguageOutlinesJsonFileToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideService(typeof(VisualStudioContextMenuCommandBridge), IsAsyncQueryable = true)]
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
                LoadDlls(
                    [
                        "Xceed.Wpf.Toolkit.dll",
                        "System.ClientModel.dll",
                        "JsonPath.Net.dll"
                    ]);

                AddService(
                    typeof(VisualStudioContextMenuCommandBridge),
                    (_, _, _) => Task.FromResult<object>(new VisualStudioContextMenuCommandBridge()),
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

                EmbeddedResourceHelper.LoadXamlEmbeddedResource(
                    "FreeAIr.UI.ClickableText.ClickableTextResource.xaml"
                    );

                McpServerProxyApplication.UpdateExternalServersAsync()
                    .FileAndForget(nameof(McpServerProxyApplication.UpdateExternalServersAsync))
                    ;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
                throw;
            }
        }

        private static void LoadDlls(
            string[] dllNames
            )
        {
            foreach (var dllName in dllNames)
            {
                Assembly a = Assembly.LoadFrom(System.IO.Path.Combine(WorkingFolder, dllName));
                AppDomain.CurrentDomain.Load(a.FullName);
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
            var gwm = componentModel.GetService<GitWindowModifier>();
            gwm.RunAsync()
                .FileAndForget(nameof(GitWindowModifier));

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