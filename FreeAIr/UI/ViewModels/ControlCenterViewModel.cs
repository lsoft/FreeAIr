using FreeAIr.Agents;
using FreeAIr.Commands.Other;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.MCP.McpServerProxy.External;
using FreeAIr.MCP.McpServerProxy.Github;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class ControlCenterViewModel : BaseViewModel
    {
        private bool? _githubMcpServerStatus;
        private string _externalMcpServerJson;
        private string _agentsJson;

        #region Model Context Protocol

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


        public string GitHubToken
        {
            get
            {
                return InternalPage.Instance.GitHubToken;
            }

            set
            {
                InternalPage.Instance.GitHubToken = value;
                InternalPage.Instance.Save();
            }
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
                if (!ExternalMcpServersJsonParser.TryParse(_externalMcpServerJson, out _))
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

        #endregion

        #region Agents

        public string AgentsJson
        {
            get => _agentsJson;
            set
            {
                _agentsJson = value;
                OnPropertyChanged();
            }
        }

        public Brush AgentsJsonBorder
        {
            get
            {
                if (!AgentCollection.TryParse(_agentsJson, out _))
                {
                    return Brushes.Red;
                }

                return Brushes.Green;
            }
        }

        public ApplyAgentsJsonCmd ApplyAgentsJsonCommand
        {
            get;
        }

        public EditAgentCmd EditAgentCommand
        {
            get;
        }

        #endregion

        public ControlCenterViewModel()
        {
            _githubMcpServerStatus = null;
            ExternalMcpServerJson = InternalPage.Instance.ExternalMCPServers;
            AgentsJson = InternalPage.Instance.Agents;

            InstallGithubMCPServerCommand = new(this);
            ApplyExternalMcpServerChangesCommand = new(this);
            EditGlobalToolsCommand = new(this);
            ApplyAgentsJsonCommand = new(this);
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
                    var toolContainer = AvailableToolContainer.ReadSystem();

                    if (await McpServerProxyCollection.ProcessMcpServerProxyAsync(toolContainer, GithubMcpServerProxy.Instance))
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

        public sealed class ApplyAgentsJsonCmd : AsyncBaseRelayCommand
        {
            private readonly ControlCenterViewModel _viewModel;

            public ApplyAgentsJsonCmd(
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
                if (!AgentCollection.TryParse(_viewModel._agentsJson, out var optionAgents))
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        $"Invalid json (1). Fix json and try again."
                        );
                    return;
                }

                try
                {
                    InternalPage.Instance.Agents = _viewModel._agentsJson;
                    await InternalPage.Instance.SaveAsync();
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
                if (string.IsNullOrEmpty(_viewModel._agentsJson))
                {
                    return false;
                }
                if (!AgentCollection.TryParse(_viewModel._agentsJson, out _))
                {
                    return false;
                }

                return true;
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
                if (!ExternalMcpServersJsonParser.TryParse(_viewModel._externalMcpServerJson, out var mcpServers))
                {
                    await VS.MessageBox.ShowErrorAsync(
                        Resources.Resources.Error,
                        $"Invalid json (1). Fix json and try again."
                        );
                    return;
                }

                try
                {
                    var setupResult = await McpServerProxyApplication.UpdateExternalServersAsync(
                        mcpServers
                        );

                    var failedServerNames = new List<string>();
                    foreach (var mcpServer in mcpServers.Servers)
                    {
                        if (setupResult.SuccessStartedMcpServers.All(a => a.Name != mcpServer.Key))
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
                        InternalPage.Instance.SaveExternalMCPServers(_viewModel._externalMcpServerJson);

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
                if (!ExternalMcpServersJsonParser.TryParse(_viewModel._externalMcpServerJson, out _))
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
                if (!AgentCollection.TryParse(_viewModel._agentsJson, out var agentCollection))
                {
                    return;
                }

                await ShowAgentConfigureCommand.ShowAsync(
                    agentCollection
                    );

                _viewModel.AgentsJson = InternalPage.Instance.Agents;

                _viewModel.OnPropertyChanged();
            }

            protected override bool CanExecuteInternal(object parameter)
            {
                if (!AgentCollection.TryParse(_viewModel._agentsJson, out _))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
