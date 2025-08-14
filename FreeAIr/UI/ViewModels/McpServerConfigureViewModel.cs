using Dto;
using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class McpServerConfigureViewModel : BaseViewModel
    {
        private McpServerWrapper _selectedServer;
        private ICommand _addNewCommand;
        private ICommand _deleteCommand;
        private ICommand _applyAndCloseCommand;
        private ICommand _upCommand;
        private ICommand _downCommand;
        private ICommand _cloneCommand;
        private ICommand _checkForConnectionCommand;
        private bool _formEnabled = true;
        private ICommand _searchCommand;

        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

        public List<McpServerWrapper> ServerCollection
        {
            get;
            private set;
        }

        public ObservableCollection2<McpServerWrapper> AvailableServers
        {
            get;
        }

        public McpServerWrapper SelectedServer
        {
            get => _selectedServer;
            set
            {
                _selectedServer = value;
                OnPropertyChanged();
            }
        }

        public bool FormEnabled
        {
            get => _formEnabled;
            set
            {
                _formEnabled = value;
                OnPropertyChanged();
            }
        }
        public Visibility ShowServerPanel
        {
            get
            {
                if (_selectedServer is null)
                {
                    return Visibility.Hidden;
                }

                return Visibility.Visible;
            }
        }

        public Brush StatusNameBorder
        {
            get
            {
                if (_selectedServer is null)
                {
                    return Brushes.Green;
                }

                if (string.IsNullOrEmpty(_selectedServer.Name))
                {
                    return Brushes.Red;
                }
                if (_selectedServer.Name.Contains(' '))
                {
                    return Brushes.Red;
                }

                return Brushes.Green;
            }
        }

        public Brush StatusJsonBorder
        {
            get
            {
                if (_selectedServer is null)
                {
                    return Brushes.Green;
                }

                if (_selectedServer.TryDeserialize() is null)
                {
                    return Brushes.Red;
                }

                return Brushes.Green;
            }
        }

        public ICommand AddNewCommand
        {
            get
            {
                if (_addNewCommand is null)
                {
                    _addNewCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await CreateServerAsync();
                        });
                }

                return _addNewCommand;
            }
        }

        public ICommand DeleteCommand
        {
            get
            {
                if (_deleteCommand is null)
                {
                    _deleteCommand = new RelayCommand(
                        a =>
                        {
                            ServerCollection.Remove(_selectedServer);
                            AvailableServers.Remove(_selectedServer);
                        },
                        a =>
                        {
                            if (_selectedServer is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _deleteCommand;
            }
        }

        public ICommand UpCommand
        {
            get
            {
                if (_upCommand is null)
                {
                    _upCommand = new RelayCommand(
                        a =>
                        {
                            var index = ServerCollection.IndexOf(_selectedServer);
                            ServerCollection.RemoveAt(index);
                            ServerCollection.Insert(index - 1, _selectedServer);
                            _selectedServer = null;

                            var aa = AvailableServers[index];
                            AvailableServers.RemoveAt(index);
                            AvailableServers.Insert(index - 1, aa);

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_selectedServer is null)
                            {
                                return false;
                            }

                            var index = ServerCollection.IndexOf(_selectedServer);
                            if (index <= 0)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _upCommand;
            }
        }

        public ICommand DownCommand
        {
            get
            {
                if (_downCommand is null)
                {
                    _downCommand = new RelayCommand(
                        a =>
                        {
                            var index = ServerCollection.IndexOf(_selectedServer);
                            ServerCollection.RemoveAt(index);
                            ServerCollection.Insert(index + 1, _selectedServer);
                            _selectedServer = null;

                            var aa = AvailableServers[index];
                            AvailableServers.RemoveAt(index);
                            AvailableServers.Insert(index + 1, aa);

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_selectedServer is null)
                            {
                                return false;
                            }

                            var index = ServerCollection.IndexOf(_selectedServer);
                            if (index >= ServerCollection.Count - 1)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _downCommand;
            }
        }

        public ICommand CloneCommand
        {
            get
            {
                if (_cloneCommand is null)
                {
                    _cloneCommand = new RelayCommand(
                        a =>
                        {
                            var clone = (McpServerWrapper)_selectedServer.Clone();
                            clone.Name += Resources.Resources.cloned;
                            ServerCollection.Add(clone);
                            AvailableServers.Add(clone);

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_selectedServer is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _cloneCommand;
            }
        }

        public ICommand SearchCommand
        {
            get
            {
                if (_searchCommand is null)
                {
                    _searchCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var w = new SearchForDockerMcpServerWindow(
                                );
                            var viewModel = new SearchForDockerMcpServerViewModel(
                                );
                            w.DataContext = viewModel;
                            var dialogResult = await w.ShowDialogAsync();
                            if (dialogResult.GetValueOrDefault())
                            {
                                if (viewModel.McpServerName != null && viewModel.McpServer != null)
                                {
                                    AddServer(
                                        viewModel.McpServerName,
                                        viewModel.McpServer
                                        );
                                }
                            }
                        });
                }

                return _searchCommand;
            }
        }

        public ICommand ApplyAndCloseCommand
        {
            get
            {
                if (_applyAndCloseCommand is null)
                {
                    _applyAndCloseCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            if (CloseWindow is not null)
                            {
                                CloseWindow(true);
                            }
                        });
                }

                return _applyAndCloseCommand;
            }
        }

        public ICommand CheckForConnectionCommand
        {
            get
            {
                if (_checkForConnectionCommand is null)
                {
                    _checkForConnectionCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            try
                            {
                                FormEnabled = false;

                                await TaskScheduler.Default;

                                var dict = BuildServerDictionary();

                                if (await FreeAIrOptions.ApplyMcpServerNodeAsync(
                                    new McpServers
                                    {
                                        Servers = dict
                                    }
                                    ))
                                {
                                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                                    await VS.MessageBox.ShowAsync(
                                        Resources.Resources.MCP_server_is_found__and_pinged_successfully,
                                        buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                                        );
                                }
                            }
                            catch (Exception excp)
                            {
                                excp.ActivityLogException();
                            }
                            finally
                            {
                                FormEnabled = true;
                            }
                        },
                        a =>
                        {
                            if (_selectedServer is null)
                            {
                                return false;
                            }
                            if (_selectedServer.TryDeserialize() is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _checkForConnectionCommand;
            }
        }

        public McpServerConfigureViewModel(
            Dictionary<string, McpServer> serverCollection
            )
        {
            ServerCollection = serverCollection.Select(p => new McpServerWrapper(this, p.Key, p.Value)).ToList();
            AvailableServers = new ObservableCollection2<McpServerWrapper>(ServerCollection);
        }

        public Dictionary<string, McpServer> BuildServerDictionary()
        {
            var result = ServerCollection
                .Select(w => (w.Name, w.TryDeserialize()))
                .Where(w => w.Item2 is not null)
                .ToDictionary(w => w.Item1, w => w.Item2)
                ;
            return result;
        }

        private void AddServer(
            string name,
            McpServer server
            )
        {
            var newServer = new McpServerWrapper(
                this,
                name,
                server
                );
            AddServerToCollections(newServer);
        }

        private async Task CreateServerAsync()
        {
            var types = new List<(string, object)>();
            foreach (McpServerType v in Enum.GetValues(typeof(McpServerType)))
            {
                var w = new McpServerTypeWrapper(v);
                types.Add((w.ToString(), w));
            }

            var chosenWrapper = await VisualStudioContextMenuCommandBridge.ShowAsync<McpServerTypeWrapper>(
                "Choose MCP server type:",
                types
                );
            if (chosenWrapper is null)
            {
                return;
            }

            var chosenType = chosenWrapper.Type;

            var newServer = new McpServerWrapper(
                this,
                DateTime.Now.ToString(),
                chosenType
                );
            AddServerToCollections(newServer);
        }

        private void AddServerToCollections(
            McpServerWrapper newServer
            )
        {
            ServerCollection.Add(newServer);
            AvailableServers.Add(newServer);
        }

        private sealed class McpServerTypeWrapper
        {
            public McpServerType Type
            {
                get;
            }

            public McpServerTypeWrapper(McpServerType type)
            {
                Type = type;
            }

            public override string ToString()
            {
                return Type.ToString();
            }
        }

        public sealed class McpServerWrapper : ICloneable
        {
            private string _name;
            private string _json;
            private readonly McpServerConfigureViewModel _viewModel;

            private string? _lastDeserializedJson = null;
            private McpServer? _lastDeserializedServer = null;

            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
                    _viewModel.OnPropertyChanged();
                }
            }

            public McpServerType Type
            {
                get;
            }

            public string Json
            {
                get => _json;
                set
                {
                    _json = value;
                    _viewModel.OnPropertyChanged();
                }
            }

            public McpServerWrapper(
                McpServerConfigureViewModel viewModel,
                string name,
                McpServer mcpServer
                )
            {
                _viewModel = viewModel;
                Name = name;
                Type = mcpServer.Type;
                Json = mcpServer.JsonConfiguration;
            }

            public McpServerWrapper(
                McpServerConfigureViewModel viewModel,
                string name,
                string json
                )
            {
                _viewModel = viewModel;
                Name = name;
                Json = json;
            }

            public McpServerWrapper(
                McpServerConfigureViewModel viewModel,
                string name,
                McpServerType type
                )
            {
                _viewModel = viewModel;
                Name = name;
                Type = type;

                switch (type)
                {
                    case McpServerType.Stdio:
                        Json =
"""
{
  "command": "my_command",
  "args": [
    "arg0",
    "arg1"
    ],
  "env": {
    "env_var0_name": "env_var0_value",
    "env_var1_name": "env_var1_value"
    }
}
""";
                        break;
                    case McpServerType.Http:
                        Json =
"""
{
    "type": "http",
    "url": "https://example.com"
}
""";
                        break;
                }
            }

            public McpServer? TryDeserialize()
            {
                try
                {
                    if (_lastDeserializedJson != Json)
                    {
                        _lastDeserializedServer = new McpServer(Type, Json);
                    }
                }
                catch
                {
                    //failed to parse json
                    //do nothing
                    _lastDeserializedServer = null;
                }
                finally
                {
                    _lastDeserializedJson = Json;
                }

                return _lastDeserializedServer;
            }

            public object Clone()
            {
                return new McpServerWrapper(
                    _viewModel,
                    Name,
                    Json
                    );
            }
        }
    }
}
