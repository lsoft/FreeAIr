using Dto;
using FreeAIr.Options2;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
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
                    _addNewCommand = new RelayCommand(
                        a =>
                        {
                            var newAgent = new McpServerWrapper(
                                this,
                                DateTime.Now.ToString()
                                );
                            ServerCollection.Add(newAgent);
                            AvailableServers.Add(newAgent);
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
                            clone.Name += " (cloned)";
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
                                        $"MCP server is found, and pinged successfully.",
                                        buttons: OLEMSGBUTTON.OLEMSGBUTTON_OK
                                        );
                                }
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
                Json = System.Text.Json.JsonSerializer.Serialize(
                    mcpServer,
                    new JsonSerializerOptions { WriteIndented = true }
                    );
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
                string name
                )
            {
                _viewModel = viewModel;
                Name = name;
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
            }

            public McpServer? TryDeserialize()
            {
                try
                {
                    if (_lastDeserializedJson != Json)
                    {
                        _lastDeserializedServer = System.Text.Json.JsonSerializer.Deserialize<McpServer>(Json);
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
