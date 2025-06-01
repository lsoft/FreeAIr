using Dto;
using FreeAIr.MCP.Agent;
using FreeAIr.MCP.Agent.External;
using FreeAIr.MCP.Agent.Github;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
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

        public ICommand InstallGithubMCPServerCommand
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

        public ICommand ApplyExternalMcpServerChangesCommand
        {
            get;
        }

        public ICommand EditGlobalToolsCommand
        {
            get;
        }

        public ControlCenterViewModel()
        {
            _githubMcpServerStatus = null;
            ExternalMcpServerJson = MCPPage.Instance.ExternalMCPServers;

            Task.Run(
                async () =>
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

                }).FileAndForget(nameof(GithubMcpServerStatusMessage));

            InstallGithubMCPServerCommand = new AsyncRelayCommand(
                async a =>
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
                            _githubMcpServerStatus = true;

                            await VS.MessageBox.ShowAsync(
                                string.Empty,
                                $"GitHub MCP server installed SUCCESSFULLY.",
                                buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                                );
                        }
                    }
                    else
                    {
                        _githubMcpServerStatus = false;

                        await VS.MessageBox.ShowErrorAsync(
                            Resources.Resources.Error,
                            $"Installation GitHub MCP server fails. Please install it manually."
                            );
                    }

                    OnPropertyChanged();
                });

            ApplyExternalMcpServerChangesCommand = new AsyncRelayCommand(
                async a =>
                {
                    if (!ExternalAgentJsonParser.TryParse(_externalMcpServerJson, out var mcpServers))
                    {
                        await VS.MessageBox.ShowErrorAsync(
                            Resources.Resources.Error,
                            $"Invalid json (1). Fix json and try again."
                            );
                        return;
                    }

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
                        MCPPage.Instance.ExternalMCPServers = _externalMcpServerJson;
                        await MCPPage.Instance.SaveAsync();

                        setupResult.ToolContainer.SaveToSystem();

                        await VS.MessageBox.ShowAsync(
                            $"Every MCP server is found, and pinged successfully. Changes was saved.",
                            buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                            );
                    }
                },
                b =>
                {
                    if (string.IsNullOrEmpty(_externalMcpServerJson))
                    {
                        return false;
                    }
                    if (!ExternalAgentJsonParser.TryParse(_externalMcpServerJson, out _))
                    {
                        return false;
                    }

                    return true;
                }
                );

            EditGlobalToolsCommand = new AsyncRelayCommand(
                async a =>
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

                    OnPropertyChanged();
                }
                );
        }
    }
}
