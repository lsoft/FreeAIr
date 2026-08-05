using Dto;
using FreeAIr.Helper;
using FreeAIr.Options2;
using FreeAIr.UI.ContextMenu;
using FreeAIr.UI.Windows;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// View model backing the MCP server configuration window, where the user adds, edits,
    /// reorders and validates the MCP servers (stdio or HTTP) stored in the FreeAIr options.
    /// </summary>
    public sealed class McpServerConfigureViewModel : BaseViewModel
    {
        /// <summary>
        /// The MCP server currently chosen in the list, or null when none is selected.
        /// </summary>
        private McpServerWrapper _selectedServer;

        /// <summary>
        /// Callback invoked to close the configuration window, passing the dialog result
        /// (true if the user applied their changes, false if they cancelled).
        /// </summary>
        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

        /// <summary>
        /// The full set of MCP servers being edited in this window, backing the persisted
        /// server dictionary saved to the FreeAIr options.
        /// </summary>
        public List<McpServerWrapper> ServerCollection
        {
            get;
            private set;
        }

        /// <summary>
        /// The observable projection of <see cref="ServerCollection"/> bound to the server
        /// list UI, kept in sync as servers are added, removed, cloned or reordered.
        /// </summary>
        public ObservableCollection2<McpServerWrapper> AvailableServers
        {
            get;
        }

        /// <summary>
        /// The MCP server the user has selected in the list, driving which server's
        /// name/JSON panel is shown for editing.
        /// </summary>
        public McpServerWrapper SelectedServer
        {
            get => _selectedServer;
            set
            {
                _selectedServer = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether the configuration form is interactive; set to false while a long-running
        /// operation such as the connection check is in progress, to keep the user from
        /// editing the server list mid-operation.
        /// </summary>
        public bool FormEnabled
        {
            get;
            set
            {
                field = value;
                OnPropertyChanged();
            }
        } = true;

        /// <summary>
        /// Visibility of the server detail panel, shown only once a server is selected in
        /// the list so the window starts with an empty right-hand side.
        /// </summary>
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

        /// <summary>
        /// Border color for the server name field, turning red when the name is empty or
        /// contains a space so the user gets immediate validation feedback.
        /// </summary>
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

        /// <summary>
        /// Border color for the server JSON configuration field, turning red when the JSON
        /// fails to deserialize into a valid MCP server definition.
        /// </summary>
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

        /// <summary>
        /// Command that prompts the user to pick an MCP server type (stdio or HTTP) and adds
        /// a new server pre-filled with a sample JSON template to the list.
        /// </summary>
        public ICommand AddNewCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            await CreateServerAsync();
                        });
                }

                return field;
            }
        }

        /// <summary>
        /// Command that removes the currently selected server from both the working
        /// collection and the bound list; disabled when nothing is selected.
        /// </summary>
        public ICommand DeleteCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Command that moves the selected server one position earlier in the list, letting
        /// the user control the order servers are shown and applied in.
        /// </summary>
        public ICommand UpCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Command that moves the selected server one position later in the list, the
        /// counterpart to <see cref="UpCommand"/> for reordering servers.
        /// </summary>
        public ICommand DownCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Command that duplicates the selected server, appending a "cloned" suffix to its
        /// name, as a quick way to base a new server configuration on an existing one.
        /// </summary>
        public ICommand CloneCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Command that opens the Docker MCP server search window, letting the user browse
        /// the Docker MCP catalog and add a chosen server straight into this configuration.
        /// </summary>
        public ICommand SearchCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Command that closes the window with a positive dialog result, signalling the
        /// caller to persist the edited MCP server list to the FreeAIr options.
        /// </summary>
        public ICommand ApplyAndCloseCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            if (CloseWindow is not null)
                            {
                                CloseWindow(true);
                            }
                        });
                }

                return field;
            }
        }

        /// <summary>
        /// Command that pings the selected server by applying the current server dictionary
        /// through <see cref="FreeAIrOptions.ApplyMcpServerNodeAsync"/> and reports success
        /// via a message box, so the user can verify a server is reachable before saving.
        /// </summary>
        public ICommand CheckForConnectionCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        /// <summary>
        /// Builds this view model from the MCP server dictionary read out of the FreeAIr
        /// options, wrapping each entry so the UI can edit it independently.
        /// </summary>
        public McpServerConfigureViewModel(
            Dictionary<string, McpServer> serverCollection
            )
        {
            ServerCollection = serverCollection.Select(p => new McpServerWrapper(this, p.Key, p.Value)).ToList();
            AvailableServers = new ObservableCollection2<McpServerWrapper>(ServerCollection);
        }

        /// <summary>
        /// Converts the edited server list back into the name-to-server dictionary shape
        /// the FreeAIr options persist, dropping any server whose JSON fails to deserialize.
        /// </summary>
        public Dictionary<string, McpServer> BuildServerDictionary()
        {
            var result = ServerCollection
                .Select(w => (w.Name, w.TryDeserialize()))
                .Where(w => w.Item2 is not null)
                .ToDictionary(w => w.Item1, w => w.Item2)
                ;
            return result;
        }

        /// <summary>
        /// Wraps a server produced elsewhere (e.g. picked from the Docker MCP catalog search)
        /// and adds it to both the working collection and the bound list.
        /// </summary>
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

        /// <summary>
        /// Handles the "add new server" flow: asks the user to pick an MCP server type via
        /// a context-menu style picker, then creates a new server of that type with a
        /// default name and adds it to the list.
        /// </summary>
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

        /// <summary>
        /// Shared tail of the add-server flows: appends the new wrapper to both
        /// <see cref="ServerCollection"/> and <see cref="AvailableServers"/> so the model and
        /// the UI stay in sync.
        /// </summary>
        private void AddServerToCollections(
            McpServerWrapper newServer
            )
        {
            ServerCollection.Add(newServer);
            AvailableServers.Add(newServer);
        }

        /// <summary>
        /// Adapts an <see cref="McpServerType"/> enum value for display in the "choose MCP
        /// server type" picker shown by <see cref="CreateServerAsync"/>.
        /// </summary>
        private sealed class McpServerTypeWrapper
        {
            /// <summary>
            /// The MCP server type (stdio or HTTP) this picker entry represents.
            /// </summary>
            public McpServerType Type
            {
                get;
            }

            /// <summary>
            /// Wraps the given server type for display in the type picker.
            /// </summary>
            public McpServerTypeWrapper(McpServerType type)
            {
                Type = type;
            }

            /// <summary>
            /// The text shown for this entry in the type picker list.
            /// </summary>
            public override string ToString()
            {
                return Type.ToString();
            }
        }

        /// <summary>
        /// Editable representation of a single MCP server entry in the configuration window:
        /// its name, type and raw JSON configuration, plus cached JSON deserialization used
        /// to drive the validation borders and the connection check.
        /// </summary>
        public sealed class McpServerWrapper : ICloneable
        {
            /// <summary>
            /// The server's display/lookup name as it will be saved in the MCP server
            /// dictionary.
            /// </summary>
            private string _name;

            /// <summary>
            /// The server's raw JSON configuration text as edited by the user.
            /// </summary>
            private string _json;

            /// <summary>
            /// The owning configuration view model, used to raise property-changed
            /// notifications when this wrapper's fields change.
            /// </summary>
            private readonly McpServerConfigureViewModel _viewModel;

            /// <summary>
            /// The JSON text that produced <see cref="_lastDeserializedServer"/>, used to
            /// avoid re-parsing on every access of <see cref="TryDeserialize"/>.
            /// </summary>
            private string? _lastDeserializedJson = null;

            /// <summary>
            /// Cached result of the last successful JSON deserialization, or null if the
            /// current JSON does not parse into a valid MCP server.
            /// </summary>
            private McpServer? _lastDeserializedServer = null;

            /// <summary>
            /// The server's display/lookup name shown in the server list and used as the key
            /// when the servers are saved back to the FreeAIr options.
            /// </summary>
            public string Name
            {
                get => _name;
                set
                {
                    _name = value;
                    _viewModel.OnPropertyChanged();
                }
            }

            /// <summary>
            /// Whether this server connects over stdio or HTTP, which determines the sample
            /// JSON template offered when the server is first created.
            /// </summary>
            public McpServerType Type
            {
                get;
            }

            /// <summary>
            /// The server's raw JSON configuration text as edited by the user; setting it
            /// invalidates the cached deserialization used by <see cref="TryDeserialize"/>.
            /// </summary>
            public string Json
            {
                get => _json;
                set
                {
                    _json = value;
                    _viewModel.OnPropertyChanged();
                }
            }

            /// <summary>
            /// Wraps an existing MCP server (loaded from the FreeAIr options) for editing.
            /// </summary>
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

            /// <summary>
            /// Wraps a server by raw name and JSON, used when cloning an existing wrapper.
            /// </summary>
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

            /// <summary>
            /// Creates a brand-new server of the given type, pre-filling <see cref="Json"/>
            /// with a sample stdio or HTTP configuration template for the user to edit.
            /// </summary>
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

            /// <summary>
            /// Parses <see cref="Json"/> into an <see cref="McpServer"/>, caching the result
            /// keyed on the JSON text so repeated calls (e.g. from the validation borders)
            /// don't re-parse unchanged text; returns null when the JSON is invalid.
            /// </summary>
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

            /// <summary>
            /// Creates an independent copy of this server wrapper, used by
            /// <see cref="CloneCommand"/> to duplicate a server entry.
            /// </summary>
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
