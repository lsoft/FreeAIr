using FreeAIr.MCP.McpServerProxy;
using FreeAIr.MCP.McpServerProxy.Github;
using FreeAIr.Options2;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
                    return "Waiting for status";
                }

                if (_githubMcpServerStatus.Value)
                {
                    return "Installed and Ready";
                }

                return "Not Installed";
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
                    return "Current json is different from the saved one. Consider saving your changes.";
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
                    "Json cannot be deserialized because of:"
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
                            $"GitHub MCP server installed SUCCESSFULLY.",
                            buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                            );
                    }
                }
                else
                {
                    _viewModel._githubMcpServerStatus = false;

                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        $"Installation GitHub MCP server fails. Please install it manually."
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
                                $"Options json is not empty. Overwrite?"
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
                    //todo log
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
                            $"Options json is not empty. Overwrite?"
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
                                $"FreeAIr options file does not found: {filePath}"
                                );
                            return;
                        }
                    }
                    else if (place.HasValue && place.Value == OptionsPlaceEnum.VisualStudioOption)
                    {
                        if (string.IsNullOrEmpty(InternalPage.Instance.Options))
                        {
                            await VS.MessageBox.ShowErrorAsync(
                                $"Visual studio has no active FreeAIr options."
                                );
                            return;
                        }
                    }

                    await _viewModel.AcceptOptionsAsync();
                }
                catch (Exception excp)
                {
                    //todo log
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
                        $"Invalid json. Fix json and try again."
                        );
                    return;
                }

                try
                {
                    if (!await ApplyMcpServerNodeAsync(options))
                    {
                        return;
                    }

                    var place = _viewModel.SelectedPlace.Place;

                    _ = await options.SerializeAsync(place);

                    await _viewModel.AcceptOptionsAsync();

                    await VS.MessageBox.ShowAsync(
                        $"Every MCP server is found, and pinged successfully. Changes was saved successfully.",
                        buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                        );
                }
                catch (Exception excp)
                {
                    //todo log
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


            private async Task<bool> ApplyMcpServerNodeAsync(
                FreeAIrOptions options
                )
            {
                if (options is null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                var setupResult = await McpServerProxyApplication.UpdateExternalServersAsync(
                    options.AvailableMcpServers
                    );
                if (setupResult is null)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        $"Invalid MCP servers json subnode. Fix json and try again."
                        );
                    return false;
                }

                var failedServerNames = new List<string>();
                foreach (var mcpServer in options.AvailableMcpServers.Servers)
                {
                    if (setupResult.SuccessStartedMcpServers.All(a => a.Name != mcpServer.Key))
                    {
                        //этот сервер не был инициализирован по какой-то причине
                        failedServerNames.Add(mcpServer.Key);
                    }
                }
                if (failedServerNames.Count > 0)
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        $"Some MCP servers failed to start: {string.Join(",", failedServerNames)}. Changes did not saved."
                        );
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
                        "You are going to delete stored options. Please, confirm.")
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
                                $"Options file {filePath} has been deleted.",
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
                            $"Options inside Visual Studio has been deleted.",
                            buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                            );
                    }

                }
                catch (Exception excp)
                {
                    //todo log
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        excp.Message + Environment.NewLine + excp.StackTrace
                        );
                }
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
