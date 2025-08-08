using FreeAIr.Helper;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.MCP.McpServerProxy.Github;
using FreeAIr.Options2;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.VSHelp80;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class ControlCenterViewModel : BaseViewModel
    {
        private readonly CachedDeserializer _cachedDeserializer = new();

        private bool? _githubMcpServerStatus;
        private string _optionsJson;
        private string _originalJson;
        private PlaceViewModel _selectedPlace;

        public bool PageEnabled
        {
            get;
            set;
        }

        public ObservableCollection2<PlaceViewModel> PlaceList
        {
            get;
        }

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

        public InstallGithubMCPServerCmd InstallGithubMCPServerCommand
        {
            get;
        }

        public string OptionsJson
        {
            get => _optionsJson;
            set
            {
                _optionsJson = value;
                OnPropertyChanged();
            }
        }

        public Brush StatusJsonBorder
        {
            get
            {
                if (!_cachedDeserializer.TryDeserializeFromString(_optionsJson, out _, out _))
                {
                    return Brushes.Red;
                }

                if (_originalJson != _optionsJson)
                {
                    return Brushes.Orange;
                }

                return Brushes.Green;
            }
        }

        public string OptionsJsonError
        {
            get
            {
                if (!_cachedDeserializer.TryDeserializeFromString(_optionsJson, out _, out var errorMessage))
                {
                    return errorMessage;
                }

                if (_originalJson != _optionsJson)
                {
                    return FreeAIr.Resources.Resources.Current_json_is_different_from_the;
                }

                return string.Empty;
            }
        }

        public Visibility OptionsJsonVisibility
        {
            get
            {
                if (!_cachedDeserializer.TryDeserializeFromString(_optionsJson, out _, out _))
                {
                    return Visibility.Visible;
                }
                if (_originalJson != _optionsJson)
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }
        }

        public SetDefaultOptionsCmd SetDefaultOptionsCommand
        {
            get;
        }

        public LoadOptionsCmd LoadOptionsCommand
        {
            get;
        }

        public StoreOptionsCmd StoreOptionsCommand
        {
            get;
        }

        public DeleteOptionsCmd DeleteOptionsCommand
        {
            get;
        }

        public EditMcpServersCmd EditMcpServersCommand
        {
            get;
        }

        public EditGlobalToolsCmd EditGlobalToolsCommand
        {
            get;
        }

        public EditAgentCmd EditAgentCommand
        {
            get;
        }

        public EditActionCmd EditActionCommand
        {
            get;
        }

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

        private void OnGithubPropertyChanged()
        {
            OnPropertyChanged(nameof(GithubMcpServerStatusMessage));
            OnPropertyChanged(nameof(InstallGithubMCPServerCommand));
        }

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

        public sealed class InstallGithubMCPServerCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

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

        public sealed class SetDefaultOptionsCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

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

        public sealed class LoadOptionsCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

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
                        var filePath = await FreeAIrOptions.ComposeFilePathAsync();
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

            protected override bool CanExecuteInternal(object parameter)
            {
                return true;
            }
        }

        public sealed class StoreOptionsCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

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

        public sealed class DeleteOptionsCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

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
                        var filePath = await FreeAIrOptions.ComposeFilePathAsync();
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

        public sealed class EditMcpServersCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

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

            protected override bool CanExecuteInternal(object parameter)
            {
                if (string.IsNullOrEmpty(_viewModel._optionsJson))
                {
                    return false;
                }

                return _viewModel._cachedDeserializer.TryDeserializeFromString(_viewModel._optionsJson, out _, out _);
            }
        }

        public sealed class EditGlobalToolsCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

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

            protected override bool CanExecuteInternal(object parameter)
            {
                if (string.IsNullOrEmpty(_viewModel._optionsJson))
                {
                    return false;
                }

                return _viewModel._cachedDeserializer.TryDeserializeFromString(_viewModel._optionsJson, out _, out _);
            }
        }

        public sealed class EditAgentCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

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

            protected override async Task ExecuteInternalAsync(object parameter)
            {
                var optionJson = _viewModel._optionsJson;

                var options = FreeAIrOptions.DeserializeFromString(
                    optionJson
                    );

                var w = new AgentConfigureWindow(
                    );
                w.DataContext = new AgentConfigureViewModel(
                    options.AgentCollection
                    );
                if ((await w.ShowDialogAsync()).GetValueOrDefault())
                {
                    _viewModel._optionsJson = FreeAIrOptions.SerializeToString(options);
                }

                _viewModel.OnPropertyChanged();
            }

            protected override bool CanExecuteInternal(object parameter)
            {
                if (string.IsNullOrEmpty(_viewModel._optionsJson))
                {
                    return false;
                }

                return _viewModel._cachedDeserializer.TryDeserializeFromString(_viewModel._optionsJson, out _, out _);
            }
        }

        public sealed class EditActionCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

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

            protected override async Task ExecuteInternalAsync(object parameter)
            {
                var optionJson = _viewModel._optionsJson;

                var options = FreeAIrOptions.DeserializeFromString(
                    optionJson
                    );

                var w = new ActionConfigureWindow(
                    );
                w.DataContext = new ActionConfigureViewModel(
                    options.Supports
                    );
                if ((await w.ShowDialogAsync()).GetValueOrDefault())
                {
                    _viewModel._optionsJson = FreeAIrOptions.SerializeToString(options);
                }

                _viewModel.OnPropertyChanged();
            }

            protected override bool CanExecuteInternal(object parameter)
            {
                if (string.IsNullOrEmpty(_viewModel._optionsJson))
                {
                    return false;
                }

                return _viewModel._cachedDeserializer.TryDeserializeFromString(_viewModel._optionsJson, out _, out _);
            }
        }

        public sealed class CachedDeserializer
        {
            private string? _optionsJson;

            private bool _result;
            private FreeAIrOptions? _options;
            private string? _errorMessage;

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

        public sealed class PlaceViewModel
        {
            public OptionsPlaceEnum? Place
            {
                get;
            }

            public string Title => Place.GetTitle();

            public PlaceViewModel(
                OptionsPlaceEnum? place
                )
            {
                Place = place;
            }

        }
    }

}
