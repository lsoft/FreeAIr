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
using FreeAIr.Record;
using FreeAIr.UI;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Informer;
using FreeAIr.UI.ToolWindows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace FreeAIr
{
    /// <summary>
    /// The extension entry point.
    ///
    /// The package auto-loads both with and without a solution, because parts of FreeAIr (the chat
    /// list, the control center, the MCP proxy) must be usable before any solution is opened.
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.FreeAIrString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(OptionsProvider.UIPageOptions), "FreeAIr", "UI", 0, 0, true, SupportsProfiles = true)]
    [ProvideOptionPage(typeof(OptionsProvider.InternalPageOptions), "FreeAIr", "Internal", 0, 0, true, SupportsProfiles = true)]
    [ProvideOptionPage(typeof(OptionsProvider.FontSizePageOptions), "FreeAIr", "Font sizes", 0, 0, true, SupportsProfiles = true)]
    [ProvideOptionPage(typeof(OptionsProvider.RecordingPageOptions), "FreeAIr", "Recording audio", 0, 0, true, SupportsProfiles = true)]
    [ProvideToolWindow(typeof(ChatListToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(ChooseModelToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(NaturalLanguageResultsToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(NaturalLanguageOutlinesToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(BuildNaturalLanguageOutlinesJsonFileToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideToolWindow(typeof(RagCalibrationToolWindow.Pane), Style = VsDockStyle.Tabbed, Window = WindowGuids.DocumentWell)]
    [ProvideService(typeof(VisualStudioContextMenuCommandBridge), IsAsyncQueryable = true)]
    public sealed class FreeAIrPackage : ToolkitPackage
    {
        /// <summary>
        /// The single running instance of the package, set as soon as the constructor runs so
        /// static helpers throughout FreeAIr can reach package services (e.g. `GetServiceAsync`)
        /// without needing it passed down explicitly.
        /// </summary>
        public static FreeAIrPackage Instance = null;

        /// <summary>
        /// Folder the extension assembly was loaded from. Everything shipped inside the VSIX
        /// (the MCP proxy, the Whisper runtimes, ...) is unpacked relative to it.
        /// </summary>
        public static readonly string WorkingFolder;

        /// <summary>
        /// Raised for every WPF window loaded in devenv, not only FreeAIr's own ones.
        /// The in situ chat listens to this pair to give up its topmost state while some other
        /// window is open, and to take it back afterwards.
        /// </summary>
        public static event Action<Window>? WindowOpened;

        /// <inheritdoc cref="WindowOpened"/>
        public static event Action<Window>? WindowClosed;

        /// <summary>
        /// Resolves <see cref="WorkingFolder"/> from the executing assembly's location once,
        /// before any package instance exists.
        /// </summary>
        static FreeAIrPackage()
        {
            var eal = Assembly.GetExecutingAssembly().Location;
            var exeFolderPath = new System.IO.FileInfo(eal).Directory.FullName;
            WorkingFolder = exeFolderPath;
        }

        /// <summary>
        /// Records this package as the running <see cref="Instance"/>; Visual Studio constructs
        /// exactly one of these.
        /// </summary>
        public FreeAIrPackage(
            )
        {
            Instance = this;
        }

        /// <summary>
        /// Wires up everything the extension needs.
        ///
        /// Most of the long-running parts are started with `FileAndForget` on purpose: package
        /// initialization blocks Visual Studio, so nothing here is allowed to wait for a network,
        /// a child process or a background scan.
        /// </summary>
        protected override async Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress
            )
        {
            try
            {
                //first of all, so that anything failing below is on the record: the assemblies which
                //know nothing about Visual Studio report through a hook, and until it is attached
                //they report into nothing
                AttachDiagnosticSinks();

                //load dlls manually, for unknown reason these dlls does not loaded automatically
                LoadDlls(
                    [
                        "Xceed.Wpf.Toolkit.dll",
                        "System.ClientModel.dll",
                    ]);

                // Handle Loaded for every Window in the app
                EventManager.RegisterClassHandler(
                    typeof(Window),
                    FrameworkElement.LoadedEvent,
                    new RoutedEventHandler(OnAnyWindowLoaded)
                    );

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

                await ChosenRecorder.InitAsync();

                if (!ShowSetupWizardInfoBarIfNeeded())
                {
                    ShowReleaseNotesInfoBarIfNeeded();
                }

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

        #region window actions

        /// <summary>
        /// Raises <see cref="WindowOpened"/> for every WPF window loaded anywhere in devenv, and
        /// (re)subscribes to that window's Closed event so it is reported exactly once even
        /// though Loaded can fire multiple times for the same window.
        /// </summary>
        private void OnAnyWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is not Window w)
            {
                return;
            }

            // If you want the moment it’s actually rendered:
            //w.ContentRendered += (_, __) => WindowOpened?.Invoke(w);

            // Or, if "Loaded" is good enough:
            WindowOpened?.Invoke(w);

            // track close. Loaded fires again every time the window is re-attached to the visual
            // tree, so the handler has to be dropped first — otherwise a single Closed would be
            // reported as many times as the window has been loaded
            w.Closed -= AnyWindowClosed;
            w.Closed += AnyWindowClosed;
        }

        /// <summary>
        /// Raises <see cref="WindowClosed"/> for the given window and unsubscribes itself so the
        /// handler does not fire again if the window is reused.
        /// </summary>
        private void AnyWindowClosed(object sender, EventArgs e)
        {
            if (sender is not Window w)
            {
                return;
            }

            try
            {
                WindowClosed?.Invoke(w);
            }
            finally
            {
                w.Closed -= AnyWindowClosed;
            }
        }

        #endregion

        /// <summary>
        /// Points the assemblies which cannot reach Visual Studio at the activity log.
        ///
        /// <c>FreeAIr.Llm</c> and <c>WpfHelpers</c> are netstandard2.0 and know nothing about the
        /// IDE, so each reports through a hook of its own which does nothing until it is attached.
        /// What they report is precisely what is otherwise invisible: a wire protocol quietly
        /// recovering from a malformed payload, and a command failure the user saw once in a
        /// message box and then closed.
        ///
        /// The activity log itself is only written when Visual Studio was started with `/log`,
        /// which is worth telling anyone who is asked to reproduce a problem.
        /// </summary>
        private static void AttachDiagnosticSinks()
        {
            FreeAIr.Llm.LlmDiagnostics.Sink = ActivityLogHelper.ActivityLogWarning;
            WpfHelpers.CommandDiagnostics.Sink = excp => excp.ActivityLogException("A command has failed.");
        }

        /// <summary>
        /// Explicitly loads the given DLLs from <see cref="WorkingFolder"/> into the app domain,
        /// working around dependencies (Xceed WPF Toolkit, System.ClientModel) that the normal
        /// assembly resolution does not pick up automatically inside devenv.
        /// </summary>
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

        /// <summary>
        /// Shows the release-notes info bar once per new FreeAIr version: compares the running
        /// VSIX version against the last version recorded in <see cref="InternalPage"/> and
        /// displays the bar only when they differ.
        /// </summary>
        private static void ShowReleaseNotesInfoBarIfNeeded()
        {
            ActivityLogHelper.ActivityLogInformation(
                $"Release notes info bar: running {Vsix.Version}, last seen {InternalPage.Instance.FreeAIrLastVersion ?? "<none>"}."
                );

            if (Vsix.Version != InternalPage.Instance.FreeAIrLastVersion)
            {
                var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
                var sp = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte);
                ReleaseNotesInfoBarService.Initialize(sp);
                ReleaseNotesInfoBarService.Instance.ShowInfoBar();
            }
        }

        /// <summary>
        /// Shows the first-run info bar (offering release notes plus the setup wizard) exactly
        /// once per install, before <see cref="InternalPage.SetupWizardIntroduced"/> is set: it
        /// supersedes the plain release-notes bar on this run, since its own action already opens
        /// the release notes too. Returns whether it was shown, so the caller can fall back to the
        /// ordinary "new version installed" bar otherwise.
        /// </summary>
        private static bool ShowSetupWizardInfoBarIfNeeded()
        {
            ActivityLogHelper.ActivityLogInformation(
                $"Setup wizard info bar: already introduced = {InternalPage.Instance.SetupWizardIntroduced}."
                );

            if (InternalPage.Instance.SetupWizardIntroduced)
            {
                return false;
            }

            //this runs during package initialization: failing to show a bar is worth logging, but
            //never worth failing the load of the whole extension over
            try
            {
                var dte = AsyncPackage.GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
                var sp = new ServiceProvider((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte);
                SetupWizardInfoBarService.Initialize(sp);
                SetupWizardInfoBarService.Instance.ShowInfoBar();
                return true;
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
                return false;
            }
        }

        /// <summary>
        /// Starts the package's background MEF services (the git window modifier and the UI
        /// informer) and wires the informer's double-click event to open the chat list.
        /// </summary>
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

        /// <summary>
        /// Opens the chat list tool window when the user double-clicks the FreeAIr status/info UI.
        /// </summary>
        private static async void UIDoubleClickEvent(object sender, EventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                _ = await ChatListToolWindow.ShowAsync();
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }
    }
}