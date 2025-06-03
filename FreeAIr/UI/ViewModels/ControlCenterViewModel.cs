using FreeAIr.MCP.Agent;
using FreeAIr.MCP.Agent.External;
using FreeAIr.MCP.Agent.Github;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class ControlCenterViewModel : BaseViewModel
    {
        private bool? _githubMcpServerStatus;
        private string _externalMcpServerJson;

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

        public string ExternalMcpServerJson
        {
            get => _externalMcpServerJson;
            set
            {
                _externalMcpServerJson = value;
                OnPropertyChanged();
            }
        }

        public Brush ExternalMcpServerJsonBorder
        {
            get
            {
                if (!ExternalAgentJsonParser.TryParse(_externalMcpServerJson, out _))
                {
                    return Brushes.Red;
                }

                return Brushes.Green;
            }
        }

        public ApplyExternalMcpServerChangesCmd ApplyExternalMcpServerChangesCommand
        {
            get;
        }

        public EditGlobalToolsCmd EditGlobalToolsCommand
        {
            get;
        }

        public ControlCenterViewModel()
        {
            _githubMcpServerStatus = null;
            ExternalMcpServerJson = MCPPage.Instance.ExternalMCPServers;

            InstallGithubMCPServerCommand = new(this);
            ApplyExternalMcpServerChangesCommand = new(this);
            EditGlobalToolsCommand = new(this);

            UpdateGithubMcpStatusAsync()
                .FileAndForget(nameof(UpdateGithubMcpStatusAsync));
        }

        private async Task UpdateGithubMcpStatusAsync()
        {
            var isInstalled = await GithubAgent.Instance.IsInstalledAsync();
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
                    var toolContainer = AvailableToolContainer.ReadSystem();

                    if (await AgentCollection.ProcessAgentAsync(toolContainer, GithubAgent.Instance))
                    {
                        toolContainer.SaveToSystem();
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

        public sealed class ApplyExternalMcpServerChangesCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

            public ApplyExternalMcpServerChangesCmd(
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
                if (!ExternalAgentJsonParser.TryParse(_viewModel._externalMcpServerJson, out var mcpServers))
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        $"Invalid json (1). Fix json and try again."
                        );
                    return;
                }

                try
                {
                    var setupResult = await AgentApplication.UpdateExternalServersAsync(
                        mcpServers
                        );

                    var failedServerNames = new List<string>();
                    foreach (var mcpServer in mcpServers.Servers)
                    {
                        if (setupResult.SuccessStartedAgents.All(a => a.Name != mcpServer.Key))
                        {
                            //этот сервер не был инициализирован по какой-то причине
                            failedServerNames.Add(mcpServer.Key);
                        }
                    }

                    if (setupResult is null)
                    {
                        await VS.MessageBox.ShowErrorAsync(
                            Resources.Resources.Error,
                            $"Invalid json (2). Fix json and try again."
                            );
                    }
                    if (failedServerNames.Count > 0)
                    {
                        await VS.MessageBox.ShowErrorAsync(
                            Resources.Resources.Error,
                            $"Some MCP servers failed to start: {string.Join(",", failedServerNames)}. Changes did not saved."
                            );
                    }
                    else
                    {
                        MCPPage.Instance.ExternalMCPServers = _viewModel._externalMcpServerJson;
                        await MCPPage.Instance.SaveAsync();

                        setupResult.ToolContainer.SaveToSystem();

                        await VS.MessageBox.ShowAsync(
                            $"Every MCP server is found, and pinged successfully. Changes was saved.",
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

            protected override bool CanExecuteInternal(object parameter)
            {
                if (string.IsNullOrEmpty(_viewModel._externalMcpServerJson))
                {
                    return false;
                }
                if (!ExternalAgentJsonParser.TryParse(_viewModel._externalMcpServerJson, out _))
                {
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
                var toolContainer = AvailableToolContainer.ReadSystem();

                var w = new NestedCheckBoxWindow();
                w.DataContext = new AvailableToolsViewModel(
                    toolContainer
                    );
                if ((await w.ShowDialogAsync()).GetValueOrDefault())
                {
                    toolContainer.SaveToSystem();
                }

                _viewModel.OnPropertyChanged();
            }
        }
    }
}
