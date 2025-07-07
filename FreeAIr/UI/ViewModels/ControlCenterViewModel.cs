using FreeAIr.MCP.McpServerProxy;
using FreeAIr.MCP.McpServerProxy.Github;
using FreeAIr.Options2;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WpfHelpers;
using static FreeAIr.Options2.FreeAIrOptions;

namespace FreeAIr.UI.ViewModels
{
    public sealed class ControlCenterViewModel : BaseViewModel
    {
        private bool? _githubMcpServerStatus;
        private string _optionsJson;

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

        public Brush OptionsJsonBorder
        {
            get
            {
                if (FreeAIrOptions.TryDeserializeFromString(_optionsJson, out _))
                {
                    return Brushes.Green;
                }

                return Brushes.Red;
            }
        }

        public StoreOptionsCmd StoreOptionsCommand
        {
            get;
        }

        public StoreOptionsCmd StoreOptionsToFileCommand
        {
            get;
        }

        public DeleteOptionsFileCmd DeleteOptionsFileCommand
        {
            get;
        }

        public StoreOptionsCmd StoreAsVSOptionsCommand
        {
            get;
        }

        public ClearVSOptionsCmd ClearVSOptionsCommand
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


        public ControlCenterViewModel(
            FreeAIrOptions options
            )
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _githubMcpServerStatus = null;
            OptionsJson = FreeAIrOptions.SerializeToString(
                options
                );

            InstallGithubMCPServerCommand = new(this);
            StoreOptionsCommand = new(this, null);
            StoreOptionsToFileCommand = new(this, OptionsPlaceEnum.SolutionRelatedFilePath);
            DeleteOptionsFileCommand = new(this);
            StoreAsVSOptionsCommand = new(this, OptionsPlaceEnum.VisualStudioOption);
            ClearVSOptionsCommand = new(this);
            EditGlobalToolsCommand = new(this);
            EditAgentCommand = new(this);

            UpdateGithubMcpStatusAsync()
                .FileAndForget(nameof(UpdateGithubMcpStatusAsync));
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

                _viewModel.OnPropertyChanged();
            }
        }

        public sealed class ClearVSOptionsCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

            public ClearVSOptionsCmd(
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
                    InternalPage.Instance.Options = string.Empty;

                    await VS.MessageBox.ShowAsync(
                        $"Options from Visual Studio has been deleted.",
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

                _viewModel.OnPropertyChanged();
            }

        }

        public sealed class DeleteOptionsFileCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

            public DeleteOptionsFileCmd(
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
                    var filePath = await FreeAIrOptions.ComposeFilePathAsync();
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }

                    await VS.MessageBox.ShowAsync(
                        $"Options file {filePath} has been deleted.",
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

                _viewModel.OnPropertyChanged();
            }

        }

        public sealed class StoreOptionsCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;
            private readonly OptionsPlaceEnum? _place;

            public StoreOptionsCmd(
                ControlCenterViewModel viewModel,
                OptionsPlaceEnum? place
                )
            {
                if (viewModel is null)
                {
                    throw new ArgumentNullException(nameof(viewModel));
                }

                _viewModel = viewModel;
                _place = place;
            }

            protected override async Task ExecuteInternalAsync(object parameter)
            {
                if (!FreeAIrOptions.TryDeserializeFromString(_viewModel._optionsJson, out var options))
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

                    var place = await options.SerializeAsync(_place);

                    var destination = "<<unknown>>";
                    switch (place)
                    {
                        case OptionsPlaceEnum.SolutionRelatedFilePath:
                            destination = $"file {await FreeAIrOptions.ComposeFilePathAsync()}";
                            break;
                        case OptionsPlaceEnum.VisualStudioOption:
                            destination = "VS options";
                            break;
                    }

                    await VS.MessageBox.ShowAsync(
                        $"Every MCP server is found, and pinged successfully. Changes was saved to {destination}.",
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
                if (!FreeAIrOptions.TryDeserializeFromString(_viewModel._optionsJson, out _))
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

                return FreeAIrOptions.TryDeserializeFromString(_viewModel._optionsJson, out _);
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

                return FreeAIrOptions.TryDeserializeFromString(_viewModel._optionsJson, out _);
            }
        }
    }
}
