using Dto;
using FreeAIr.Helper;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.MCP.McpServerProxy.Github;
using FreeAIr.Options2;
using FreeAIr.SetupWizard.Catalog;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// View model for the FreeAIr Control Center page: lets the user pick where options are
    /// stored (solution file, Visual Studio settings), edit the raw options JSON (MCP servers,
    /// global tools, agents, actions), and install well-known MCP servers such as GitHub and
    /// the Microsoft Docs MCP server.
    /// </summary>
    public sealed class ControlCenterViewModel : BaseViewModel
    {
        /// <summary>
        /// Caches the last successful deserialization of <see cref="OptionsJson"/> so repeated
        /// validity checks (border color, error text, command CanExecute) do not re-parse the
        /// same JSON string over and over.
        /// </summary>
        private readonly CachedDeserializer _cachedDeserializer = new();

        /// <summary>
        /// Whether the GitHub MCP server is currently detected as installed; null while the
        /// status has not been checked yet.
        /// </summary>
        private bool? _githubMcpServerStatus;

        /// <summary>
        /// The options JSON text currently shown/edited in the page.
        /// </summary>
        private string _optionsJson;

        /// <summary>
        /// The options JSON as last loaded from <see cref="SelectedPlace"/>, used to detect
        /// unsaved edits by comparison against <see cref="_optionsJson"/>.
        /// </summary>
        private string _originalJson;

        /// <summary>
        /// Backing field for <see cref="SelectedPlace"/>, the storage location whose options
        /// are being viewed/edited.
        /// </summary>
        private PlaceViewModel _selectedPlace;

        /// <summary>
        /// Whether the page's controls are enabled; disabled while options are being loaded or saved.
        /// </summary>
        public bool PageEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// The selectable options storage locations (default, solution-related file, Visual
        /// Studio option) shown in the place picker.
        /// </summary>
        public ObservableCollection2<PlaceViewModel> PlaceList
        {
            get;
        }

        /// <summary>
        /// The storage location whose options are currently displayed. Setting it disables the
        /// page and reloads <see cref="OptionsJson"/> from that location.
        /// </summary>
        public PlaceViewModel SelectedPlace
        {
            get => _selectedPlace;
            set
            {
                _selectedPlace = value;

                PageEnabled = false;

                AcceptOptionsAsync()
                    .FileAndForget(nameof(AcceptOptionsAsync));

                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Human-readable text describing whether the GitHub MCP server is installed, not
        /// installed, or still being checked, shown next to the install button.
        /// </summary>
        public string GithubMcpServerStatusMessage
        {
            get
            {
                if (!_githubMcpServerStatus.HasValue)
                {
                    return FreeAIr.Resources.Resources.Waiting_for_status;
                }

                if (_githubMcpServerStatus.Value)
                {
                    return FreeAIr.Resources.Resources.Installed_and_Ready;
                }

                return FreeAIr.Resources.Resources.Not_Installed;
            }
        }

        /// <summary>
        /// Command that downloads and installs the GitHub MCP server and registers it in the tool container.
        /// </summary>
        public InstallGithubMCPServerCmd InstallGithubMCPServerCommand
        {
            get;
        }

        /// <summary>
        /// Command that registers the Microsoft Docs (MSDN) MCP server endpoint in the current options JSON.
        /// </summary>
        public InstallMicrosoftMsdnMCPServerCmd InstallMicrosoftMsdnMCPServerCommand
        {
            get;
        }

        /// <summary>
        /// The raw FreeAIr options JSON currently displayed in the editor; changes here are
        /// validated live and drive <see cref="StatusJsonBorder"/> and <see cref="OptionsJsonError"/>.
        /// </summary>
        public string OptionsJson
        {
            get => _optionsJson;
            set
            {
                _optionsJson = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Border color for the options JSON editor reflecting its validation state: red for
        /// invalid JSON, orange for unsaved changes, green when valid and saved.
        /// </summary>
        public Brush StatusJsonBorder => GetOptionsJsonErrorAndColor().BorderColor;

        /// <summary>
        /// The current validation or unsaved-changes message for the options JSON, empty when
        /// the JSON is valid and matches what was loaded.
        /// </summary>
        public string OptionsJsonError => GetOptionsJsonErrorAndColor().Message;

        /// <summary>
        /// Whether the options JSON status/error banner should be shown, true whenever there is
        /// a validation error or unsaved-changes message to display.
        /// </summary>
        public Visibility OptionsJsonVisibility =>
            string.IsNullOrEmpty(GetOptionsJsonErrorAndColor().Message)
                ? Visibility.Collapsed
                : Visibility.Visible;

        /// <summary>
        /// Command that resets the options JSON editor to a brand-new, default <see cref="FreeAIrOptions"/> instance.
        /// </summary>
        public SetDefaultOptionsCmd SetDefaultOptionsCommand
        {
            get;
        }

        /// <summary>
        /// Command that (re)loads the options JSON from the currently selected storage location.
        /// </summary>
        public LoadOptionsCmd LoadOptionsCommand
        {
            get;
        }

        /// <summary>
        /// Command that validates and pings the configured MCP servers, then persists the
        /// options JSON to the selected storage location.
        /// </summary>
        public StoreOptionsCmd StoreOptionsCommand
        {
            get;
        }

        /// <summary>
        /// Command that deletes the stored options at the selected storage location, after
        /// user confirmation.
        /// </summary>
        public DeleteOptionsCmd DeleteOptionsCommand
        {
            get;
        }

        /// <summary>
        /// Command that opens the MCP server configuration window to add, edit or remove
        /// available MCP servers in the options JSON.
        /// </summary>
        public EditMcpServersCmd EditMcpServersCommand
        {
            get;
        }

        /// <summary>
        /// Command that opens the global tools window to enable or disable individual tools
        /// across all MCP servers in the options JSON.
        /// </summary>
        public EditGlobalToolsCmd EditGlobalToolsCommand
        {
            get;
        }

        /// <summary>
        /// Command that opens the agent configuration window to edit the agent collection in
        /// the options JSON.
        /// </summary>
        public EditAgentCmd EditAgentCommand
        {
            get;
        }

        /// <summary>
        /// Command that opens the action configuration window to edit the agent actions in the
        /// options JSON.
        /// </summary>
        public EditActionCmd EditActionCommand
        {
            get;
        }

        /// <summary>
        /// Builds the Control Center view model: populates the list of options storage
        /// locations, creates every command, kicks off the GitHub MCP status check, and loads
        /// the options JSON for the initially selected place.
        /// </summary>
        public ControlCenterViewModel(
            )
        {
            PlaceList = new ObservableCollection2<PlaceViewModel>([
                new (null),
                new (OptionsPlaceEnum.SolutionRelatedFilePath),
                new (OptionsPlaceEnum.VisualStudioOption)
                ]);
            _selectedPlace = PlaceList.First();

            _githubMcpServerStatus = null;

            _optionsJson = string.Empty;
            _originalJson = string.Empty;

            InstallGithubMCPServerCommand = new(this);
            InstallMicrosoftMsdnMCPServerCommand = new(this);

            SetDefaultOptionsCommand = new SetDefaultOptionsCmd(this);

            LoadOptionsCommand = new(this);
            StoreOptionsCommand = new(this);
            DeleteOptionsCommand = new(this);

            EditMcpServersCommand = new(this);
            EditGlobalToolsCommand = new(this);

            EditAgentCommand = new(this);
            EditActionCommand = new(this);

            UpdateGithubMcpStatusAsync()
                .FileAndForget(nameof(UpdateGithubMcpStatusAsync));

            AcceptOptionsAsync()
                .FileAndForget(nameof(AcceptOptionsAsync));
        }

        /// <summary>
        /// Validates the current options JSON and returns a status message plus the border
        /// color to show: red for a parse error or no active agents, orange when the JSON has
        /// unsaved changes, green when valid and matching the last loaded/saved copy.
        /// </summary>
        private (string Message, Brush BorderColor) GetOptionsJsonErrorAndColor()
        {
            if (!_cachedDeserializer.TryDeserializeFromString(_optionsJson, out var options, out var errorMessage))
            {
                return (errorMessage, Brushes.Red);
            }

            var filteredAgents = options.AgentCollection.FilterAgents();
            if (filteredAgents.Count == 0)
            {
                return (FreeAIr.Resources.Resources.No_active_agents_found__Make_sure, Brushes.Red);
            }

            if (_originalJson != _optionsJson)
            {
                return (FreeAIr.Resources.Resources.Current_json_is_different_from_the, Brushes.Orange);
            }

            return (string.Empty, Brushes.Green);
        }

        /// <summary>
        /// Checks whether the GitHub MCP server is installed and updates
        /// <see cref="_githubMcpServerStatus"/>, then raises property-changed on the main thread
        /// so the status message and install button refresh.
        /// </summary>
        private async Task UpdateGithubMcpStatusAsync()
        {
            var isInstalled = await GithubMcpServerProxy.Instance.IsInstalledAsync();
            if (isInstalled)
            {
                _githubMcpServerStatus = true;
            }
            else
            {
                _githubMcpServerStatus = false;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            OnGithubPropertyChanged();
        }

        /// <summary>
        /// Raises property-changed for the GitHub status message and install command so the UI
        /// reflects the latest install/status check result.
        /// </summary>
        private void OnGithubPropertyChanged()
        {
            OnPropertyChanged(nameof(GithubMcpServerStatusMessage));
            OnPropertyChanged(nameof(InstallGithubMCPServerCommand));
        }

        /// <summary>
        /// Loads and serializes the options for <see cref="SelectedPlace"/> into
        /// <see cref="OptionsJson"/>, disabling the page while it works and showing an error
        /// dialog (with an empty JSON fallback) if deserialization fails.
        /// </summary>
        private async Task AcceptOptionsAsync(
            )
        {
            try
            {
                PageEnabled = false;

                _optionsJson = FreeAIrOptions.SerializeToString(
                    await FreeAIrOptions.DeserializeAsync(
                        SelectedPlace.Place
                        )
                    );

                _originalJson = OptionsJson;
            }
            catch (Exception excp)
            {
                await VS.MessageBox.ShowErrorAsync(
                    FreeAIr.Resources.Resources.Json_cannot_be_deserialized_because
                    + Environment.NewLine
                    + excp.Message
                    );
                _optionsJson = string.Empty;
                _originalJson = string.Empty;
            }

            PageEnabled = true;

            OnPropertyChanged();
        }

        /// <summary>
        /// Command that runs the GitHub MCP server installation as a background task, shows its
        /// progress in a <see cref="WaitForTaskWindow"/>, and registers the server in the tool
        /// container on success.
        /// </summary>
        public sealed class InstallGithubMCPServerCmd : AsyncBaseRelayCommand
        {
            /// <summary>
            /// The Control Center view model whose GitHub status is updated once installation finishes.
            /// </summary>
            private readonly ControlCenterViewModel _viewModel;

            /// <summary>
            /// Creates the command bound to the given Control Center view model.
            /// </summary>
            public InstallGithubMCPServerCmd(
                ControlCenterViewModel viewModel
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;
            }

            /// <summary>
            /// Runs the GitHub MCP server install background task, and on success registers the
            /// server with the system tool container and shows a confirmation dialog; on
            /// failure shows an error dialog. Always refreshes the GitHub status properties afterward.
            /// </summary>
            protected override async Task ExecuteInternalAsync(object parameter)
            {
                var backgroundTask = new GithubMCPInstallBackgroundTask(
                    );
                var w = new WaitForTaskWindow(
                    backgroundTask
                    );
                await w.ShowDialogAsync();

                var installResult = backgroundTask.SuccessfullyInstalled;
                if (installResult)
                {
                    var toolContainer = await AvailableToolContainer.ReadSystemAsync();

                    if (await McpServerProxyCollection.ProcessMcpServerProxyAsync(toolContainer, GithubMcpServerProxy.Instance))
                    {
                        await toolContainer.SaveToSystemAsync();
                        _viewModel._githubMcpServerStatus = true;

                        await VS.MessageBox.ShowAsync(
                            string.Empty,
                            Resources.Resources.GitHub_MCP_server_installed_SUCCESSFULLY,
                            buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                            );
                    }
                }
                else
                {
                    _viewModel._githubMcpServerStatus = false;

                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        FreeAIr.Resources.Resources.Installation_GitHub_MCP_server_fails
                        );
                }

                _viewModel.OnGithubPropertyChanged();
            }
        }

        /// <summary>
        /// Command that adds or updates the Microsoft Docs (MSDN) MCP server entry in the
        /// current options JSON, pointing it at the well-known learn.microsoft.com endpoint.
        /// </summary>
        public sealed class InstallMicrosoftMsdnMCPServerCmd : AsyncBaseRelayCommand
        {
            /// <summary>
            /// The well-known HTTP endpoint of the Microsoft Docs MCP server. Taken from the shared
            /// catalog, which the setup wizard's opt-in checkbox reads too, so a server installed
            /// from here is recognized as installed there.
            /// </summary>
            const string MsdnEndpoint = KnownMcpServerCatalog.MicrosoftDocsEndpoint;

            /// <summary>
            /// The server name under which the Microsoft Docs MCP server is registered in options.
            /// </summary>
            const string MsdnServerName = KnownMcpServerCatalog.MicrosoftDocsServerName;

            /// <summary>
            /// The Control Center view model whose options JSON is updated.
            /// </summary>
            private readonly ControlCenterViewModel _viewModel;

            /// <summary>
            /// Creates the command bound to the given Control Center view model.
            /// </summary>
            public InstallMicrosoftMsdnMCPServerCmd(
                ControlCenterViewModel viewModel
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;
            }

            /// <summary>
            /// Writes (or overwrites) the Microsoft Docs MCP server entry into the deserialized
            /// options and serializes the result back into the view model's options JSON.
            /// </summary>
            protected override async Task ExecuteInternalAsync(object parameter)
            {
                var options = FreeAIrOptions.DeserializeFromString(
                    _viewModel.OptionsJson
                    );

                //именно индексатор, а не Add: имя сервера уже может быть занято - например,
                //тем же сервером, но с другим (устаревшим) endpoint-ом. Add в этом случае
                //бросает исключение, хотя команда как раз и предлагает прописать нужный endpoint
                options.AvailableMcpServers.Servers[MsdnServerName] =
                    new McpServer(
                        McpServerType.Http,
                        KnownMcpServerCatalog.BuildHttpConfiguration(MsdnEndpoint)
                        );

                _viewModel.OptionsJson = FreeAIrOptions.SerializeToString(options);
                _viewModel.OnGithubPropertyChanged();
            }

            /// <summary>
            /// Allows the command only when the options JSON parses successfully and the
            /// Microsoft Docs MCP server is not already registered with the same endpoint.
            /// </summary>
            protected override bool CanExecuteInternal(object parameter)
            {
                var success = FreeAIrOptions.TryDeserializeFromString(
                    _viewModel.OptionsJson,
                    out var options,
                    out _
                    );
                if (!success)
                {
                    return false;
                }

                if (options.AvailableMcpServers.Servers.Any(s => s.Value.IsHttpAndHasEndpoint(MsdnEndpoint)))
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Command that discards the current options JSON and replaces it with the serialized
        /// form of a brand-new <see cref="FreeAIrOptions"/>, after confirming with the user if
        /// the editor is not empty.
        /// </summary>
        public sealed class SetDefaultOptionsCmd : AsyncBaseRelayCommand
        {
            /// <summary>
            /// The Control Center view model whose options JSON is reset.
            /// </summary>
            private readonly ControlCenterViewModel _viewModel;

            /// <summary>
            /// Creates the command bound to the given Control Center view model.
            /// </summary>
            public SetDefaultOptionsCmd(
                ControlCenterViewModel viewModel
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;
            }

            /// <summary>
            /// Confirms with the user if there is existing JSON, then overwrites the options
            /// JSON with a freshly constructed default <see cref="FreeAIrOptions"/>, reporting
            /// any failure through an error dialog.
            /// </summary>
            protected override async Task ExecuteInternalAsync(object parameter)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_viewModel.OptionsJson))
                    {
                        if (!await VS.MessageBox.ShowConfirmAsync(
                                Resources.Resources.Options_json_is_not_empty__Overwrite
                                )
                            )
                        {
                            return;
                        }
                    }

                    _viewModel._optionsJson = FreeAIrOptions.SerializeToString(
                        new FreeAIrOptions()
                        );

                    _viewModel.OnPropertyChanged();
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();

                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        excp.Message + Environment.NewLine + excp.StackTrace
                        );
                }
            }

        }

        /// <summary>
        /// Command that reloads the options JSON from the currently selected storage location,
        /// confirming with the user first if there are unsaved edits, and validating that the
        /// backing file/setting actually exists before loading.
        /// </summary>
        public sealed class LoadOptionsCmd : AsyncBaseRelayCommand
        {
            /// <summary>
            /// The Control Center view model whose options JSON is reloaded.
            /// </summary>
            private readonly ControlCenterViewModel _viewModel;

            /// <summary>
            /// Creates the command bound to the given Control Center view model.
            /// </summary>
            public LoadOptionsCmd(
                ControlCenterViewModel viewModel
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;
            }

            /// <summary>
            /// Confirms overwriting non-empty JSON, verifies the selected storage location has
            /// options to load (file exists, or the VS option is non-empty), then delegates to
            /// <see cref="AcceptOptionsAsync"/> to reload; reports failures through an error dialog.
            /// </summary>
            protected override async Task ExecuteInternalAsync(object parameter)
            {
                try
                {
                    if (!string.IsNullOrEmpty(_viewModel.OptionsJson))
                    {
                        if (!await VS.MessageBox.ShowConfirmAsync(
                            Resources.Resources.Options_json_is_not_empty__Overwrite
                            )
                            )
                        {
                            return;
                        }
                    }

                    var place = _viewModel.SelectedPlace.Place;

                    if (place.HasValue && place.Value == OptionsPlaceEnum.SolutionRelatedFilePath)
                    {
                        var filePath = await FreeAIrOptions.ComposeOptionsFilePathAsync();
                        if (!File.Exists(filePath))
                        {
                            await VS.MessageBox.ShowErrorAsync(
                                FreeAIr.Resources.Resources.FreeAIr_options_file_does_not_found + $": {filePath}"
                                );
                            return;
                        }
                    }
                    else if (place.HasValue && place.Value == OptionsPlaceEnum.VisualStudioOption)
                    {
                        if (string.IsNullOrEmpty(InternalPage.Instance.Options))
                        {
                            await VS.MessageBox.ShowErrorAsync(
                                FreeAIr.Resources.Resources.Visual_studio_has_no_active_FreeAIr
                                );
                            return;
                        }
                    }

                    await _viewModel.AcceptOptionsAsync();
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();

                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        excp.Message + Environment.NewLine + excp.StackTrace
                        );
                }
            }

            /// <summary>
            /// Always allowed; loading options has no preconditions beyond what
            /// <see cref="ExecuteInternalAsync"/> itself checks.
            /// </summary>
            protected override bool CanExecuteInternal(object parameter)
            {
                return true;
            }
        }

        /// <summary>
        /// Command that pings the configured MCP servers and, once confirmed, persists the
        /// options JSON to the selected storage location.
        /// </summary>
        public sealed class StoreOptionsCmd : AsyncBaseRelayCommand
        {
            /// <summary>
            /// The Control Center view model whose options JSON is validated and saved.
            /// </summary>
            private readonly ControlCenterViewModel _viewModel;

            /// <summary>
            /// Creates the command bound to the given Control Center view model.
            /// </summary>
            public StoreOptionsCmd(
                ControlCenterViewModel viewModel
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;
            }

            /// <summary>
            /// Validates the options JSON, pings the configured MCP servers (confirming with the
            /// user if any do not respond), serializes the options to the selected storage
            /// location, reloads them, and shows a confirmation dialog; reports failures through
            /// an error dialog.
            /// </summary>
            protected override async Task ExecuteInternalAsync(object parameter)
            {
                if (!_viewModel._cachedDeserializer.TryDeserializeFromString(_viewModel._optionsJson, out var options, out _))
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        Resources.Resources.Invalid_json__Fix_json_and_try_again
                        );
                    return;
                }

                try
                {
                    _viewModel.PageEnabled = false;

                    if (!await FreeAIrOptions.ApplyMcpServerNodeAsync(
                        options.AvailableMcpServers
                        ))
                    {
                        var confirm = await VS.MessageBox.ShowConfirmAsync(
                            FreeAIr.Resources.Resources.Question,
                            FreeAIr.Resources.Resources.MCP_servers_did_not_respond__Save
                            );
                        if (!confirm)
                        {
                            return;
                        }
                    }

                    var place = _viewModel.SelectedPlace.Place;

                    _ = await options.SerializeAsync(place);

                    await _viewModel.AcceptOptionsAsync();

                    await VS.MessageBox.ShowAsync(
                        FreeAIr.Resources.Resources.Every_MCP_server_is_found__and_pinged,
                        buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                        );
                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();

                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        excp.Message + Environment.NewLine + excp.StackTrace
                        );
                }
            }

            /// <summary>
            /// Allows the command only when the options JSON is non-empty and parses successfully.
            /// </summary>
            protected override bool CanExecuteInternal(object parameter)
            {
                if (string.IsNullOrEmpty(_viewModel._optionsJson))
                {
                    return false;
                }
                if (!_viewModel._cachedDeserializer.TryDeserializeFromString(_viewModel._optionsJson, out _, out _))
                {
                    return false;
                }

                return true;
            }

        }

        /// <summary>
        /// Command that deletes the stored options at the selected location (the solution
        /// options file or the Visual Studio setting), after user confirmation.
        /// </summary>
        public sealed class DeleteOptionsCmd : AsyncBaseRelayCommand
        {
            /// <summary>
            /// The Control Center view model whose selected storage location's options are deleted.
            /// </summary>
            private readonly ControlCenterViewModel _viewModel;

            /// <summary>
            /// Creates the command bound to the given Control Center view model.
            /// </summary>
            public DeleteOptionsCmd(
                ControlCenterViewModel viewModel
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;
            }

            /// <summary>
            /// Confirms with the user, then deletes the options file (if the place is unset or
            /// the solution-related file) or clears the Visual Studio option (if the place is
            /// unset or the VS option), showing a confirmation dialog for whichever was deleted.
            /// </summary>
            protected override async Task ExecuteInternalAsync(object parameter)
            {
                try
                {
                    if (!await VS.MessageBox.ShowConfirmAsync(
                        FreeAIr.Resources.Resources.You_are_going_to_delete_stored_options
                            )
                        )
                    {
                        return;
                    }

                    var place = _viewModel.SelectedPlace.Place;

                    if (!place.HasValue || place.Value == OptionsPlaceEnum.SolutionRelatedFilePath)
                    {
                        var filePath = await FreeAIrOptions.ComposeOptionsFilePathAsync();
                        if (System.IO.File.Exists(filePath))
                        {
                            System.IO.File.Delete(filePath);

                            await VS.MessageBox.ShowAsync(
                                string.Format(
                                    FreeAIr.Resources.Resources.Options_file__0__has_been_deleted,
                                    filePath
                                    ),
                                buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                                );
                            return;
                        }

                        if (place.HasValue && place.Value == OptionsPlaceEnum.SolutionRelatedFilePath)
                        {
                            return;
                        }
                    }

                    if (!place.HasValue || place.Value == OptionsPlaceEnum.VisualStudioOption)
                    {
                        InternalPage.Instance.Options = string.Empty;

                        await VS.MessageBox.ShowAsync(
                            FreeAIr.Resources.Resources.Options_inside_Visual_Studio_has,
                            buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                            );
                    }

                }
                catch (Exception excp)
                {
                    excp.ActivityLogException();

                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        excp.Message + Environment.NewLine + excp.StackTrace
                        );
                }
            }

        }

        /// <summary>
        /// Command that opens the MCP server configuration window over the current options'
        /// available servers and writes back the edited server dictionary if confirmed.
        /// </summary>
        public sealed class EditMcpServersCmd : AsyncBaseRelayCommand
        {
            /// <summary>
            /// The Control Center view model whose options JSON is edited.
            /// </summary>
            private readonly ControlCenterViewModel _viewModel;

            /// <summary>
            /// Creates the command bound to the given Control Center view model.
            /// </summary>
            public EditMcpServersCmd(
                ControlCenterViewModel viewModel
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;
            }

            /// <summary>
            /// Opens the MCP server configuration window for the current options' server list
            /// and, if the user confirms, replaces the options' server dictionary and serializes
            /// the result back into the options JSON.
            /// </summary>
            protected override async Task ExecuteInternalAsync(object parameter)
            {
                var optionJson = _viewModel._optionsJson;

                var options = FreeAIrOptions.DeserializeFromString(optionJson);

                var w = new McpServerConfigureWindow(
                    );
                var viewModel = new McpServerConfigureViewModel(
                    options.AvailableMcpServers.Servers
                    );
                w.DataContext = viewModel;
                if ((await w.ShowDialogAsync()).GetValueOrDefault())
                {
                    options.AvailableMcpServers.Servers = viewModel.BuildServerDictionary();
                    _viewModel._optionsJson = FreeAIrOptions.SerializeToString(options);
                }

                _viewModel.OnPropertyChanged();
            }

            /// <summary>
            /// Allows the command only when the options JSON is non-empty and parses successfully.
            /// </summary>
            protected override bool CanExecuteInternal(object parameter)
            {
                if (string.IsNullOrEmpty(_viewModel._optionsJson))
                {
                    return false;
                }

                return _viewModel._cachedDeserializer.TryDeserializeFromString(_viewModel._optionsJson, out _, out _);
            }
        }

        /// <summary>
        /// Command that opens the global tools window so the user can enable or disable
        /// individual MCP tools across all configured servers, writing the change back to the
        /// options JSON if confirmed.
        /// </summary>
        public sealed class EditGlobalToolsCmd : AsyncBaseRelayCommand
        {
            /// <summary>
            /// The Control Center view model whose options JSON is edited.
            /// </summary>
            private readonly ControlCenterViewModel _viewModel;

            /// <summary>
            /// Creates the command bound to the given Control Center view model.
            /// </summary>
            public EditGlobalToolsCmd(
                ControlCenterViewModel viewModel
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;
            }

            /// <summary>
            /// Opens the nested checkbox window over the tools read from the current options and,
            /// if the user confirms, saves the updated tool selection back into the options JSON.
            /// </summary>
            protected override async Task ExecuteInternalAsync(object parameter)
            {
                var optionJson = _viewModel._optionsJson;

                var toolContainer = AvailableToolContainer.ReadFromOptions(
                    optionJson
                    );

                var w = new NestedCheckBoxWindow();
                w.DataContext = new AvailableToolsViewModel(
                    toolContainer
                    );
                if ((await w.ShowDialogAsync()).GetValueOrDefault())
                {
                    _viewModel._optionsJson = toolContainer.SaveTo(
                        optionJson
                        );
                }

                _viewModel.OnPropertyChanged();
            }

            /// <summary>
            /// Allows the command only when the options JSON is non-empty and parses successfully.
            /// </summary>
            protected override bool CanExecuteInternal(object parameter)
            {
                if (string.IsNullOrEmpty(_viewModel._optionsJson))
                {
                    return false;
                }

                return _viewModel._cachedDeserializer.TryDeserializeFromString(_viewModel._optionsJson, out _, out _);
            }
        }

        /// <summary>
        /// Command that opens the agent configuration window over the current options' agent
        /// collection and supports list, writing the edits back to the options JSON if confirmed.
        /// </summary>
        public sealed class EditAgentCmd : AsyncBaseRelayCommand
        {
            /// <summary>
            /// The Control Center view model whose options JSON is edited.
            /// </summary>
            private readonly ControlCenterViewModel _viewModel;

            /// <summary>
            /// Creates the command bound to the given Control Center view model.
            /// </summary>
            public EditAgentCmd(
                ControlCenterViewModel viewModel
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;
            }

            /// <summary>
            /// Opens the agent configuration window over the deserialized options' agent
            /// collection and supports, and if confirmed serializes the result back into the
            /// options JSON.
            /// </summary>
            protected override async Task ExecuteInternalAsync(object parameter)
            {
                var optionJson = _viewModel._optionsJson;

                var options = FreeAIrOptions.DeserializeFromString(
                    optionJson
                    );

                var w = new AgentConfigureWindow(
                    );
                w.DataContext = new AgentConfigureViewModel(
                    options.AgentCollection,
                    options.Supports
                    );
                if ((await w.ShowDialogAsync()).GetValueOrDefault())
                {
                    _viewModel._optionsJson = FreeAIrOptions.SerializeToString(options);
                }

                _viewModel.OnPropertyChanged();
            }

            /// <summary>
            /// Allows the command only when the options JSON is non-empty and parses successfully.
            /// </summary>
            protected override bool CanExecuteInternal(object parameter)
            {
                if (string.IsNullOrEmpty(_viewModel._optionsJson))
                {
                    return false;
                }

                return _viewModel._cachedDeserializer.TryDeserializeFromString(_viewModel._optionsJson, out _, out _);
            }
        }

        /// <summary>
        /// Command that opens the action configuration window over the current options' agent
        /// collection and supports list, writing the edited actions back to the options JSON if confirmed.
        /// </summary>
        public sealed class EditActionCmd : AsyncBaseRelayCommand
        {
            /// <summary>
            /// The Control Center view model whose options JSON is edited.
            /// </summary>
            private readonly ControlCenterViewModel _viewModel;

            /// <summary>
            /// Creates the command bound to the given Control Center view model.
            /// </summary>
            public EditActionCmd(
                ControlCenterViewModel viewModel
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;
            }

            /// <summary>
            /// Opens the action configuration window over the deserialized options' agent
            /// collection and supports, and if confirmed serializes the result back into the
            /// options JSON.
            /// </summary>
            protected override async Task ExecuteInternalAsync(object parameter)
            {
                var optionJson = _viewModel._optionsJson;

                var options = FreeAIrOptions.DeserializeFromString(
                    optionJson
                    );

                var w = new ActionConfigureWindow(
                    );
                w.DataContext = new ActionConfigureViewModel(
                    options.AgentCollection,
                    options.Supports
                    );
                if ((await w.ShowDialogAsync()).GetValueOrDefault())
                {
                    _viewModel._optionsJson = FreeAIrOptions.SerializeToString(options);
                }

                _viewModel.OnPropertyChanged();
            }

            /// <summary>
            /// Allows the command only when the options JSON is non-empty and parses successfully.
            /// </summary>
            protected override bool CanExecuteInternal(object parameter)
            {
                if (string.IsNullOrEmpty(_viewModel._optionsJson))
                {
                    return false;
                }

                return _viewModel._cachedDeserializer.TryDeserializeFromString(_viewModel._optionsJson, out _, out _);
            }
        }

        /// <summary>
        /// Memoizes the result of deserializing the options JSON so that repeated validity
        /// checks (border color, error message, command CanExecute) against the same string
        /// avoid re-parsing it every time.
        /// </summary>
        public sealed class CachedDeserializer
        {
            /// <summary>
            /// The options JSON string the cached result was computed for.
            /// </summary>
            private string? _optionsJson;

            /// <summary>
            /// Whether the last deserialization attempt for <see cref="_optionsJson"/> succeeded.
            /// </summary>
            private bool _result;

            /// <summary>
            /// The options deserialized from <see cref="_optionsJson"/>, or null if deserialization failed.
            /// </summary>
            private FreeAIrOptions? _options;

            /// <summary>
            /// The error message from the last failed deserialization attempt, or null if it succeeded.
            /// </summary>
            private string? _errorMessage;

            /// <summary>
            /// Deserializes the given options JSON, reusing the cached result when the JSON text
            /// is unchanged since the last call instead of parsing it again.
            /// </summary>
            public bool TryDeserializeFromString(
                string optionsJson,
                out FreeAIrOptions? options,
                out string? errorMessage
                )
            {
                if (_optionsJson != optionsJson)
                {
                    _optionsJson = optionsJson;
                    _result = FreeAIrOptions.TryDeserializeFromString(_optionsJson, out _options, out _errorMessage);
                }

                options = _options;
                errorMessage = _errorMessage;
                return _result;
            }

        }

        /// <summary>
        /// Wraps an options storage location (or the default, when null) for display in the
        /// Control Center's place picker.
        /// </summary>
        public sealed class PlaceViewModel
        {
            /// <summary>
            /// The storage location this entry represents, or null for the default location.
            /// </summary>
            public OptionsPlaceEnum? Place
            {
                get;
            }

            /// <summary>
            /// The display title for this storage location, shown in the place picker.
            /// </summary>
            public string Title => Place.GetTitle();

            /// <summary>
            /// Creates a place entry wrapping the given storage location.
            /// </summary>
            public PlaceViewModel(
                OptionsPlaceEnum? place
                )
            {
                Place = place;
            }

        }
    }

}
