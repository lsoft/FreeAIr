using FreeAIr.Options2.Agent;
using FreeAIr.UI.ContextMenu;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class AgentConfigureViewModel : BaseViewModel
    {
        private AgentJson _selectedAgent;
        private ICommand _addNewAgentCommand;
        private ICommand _deleteAgentCommand;
        private ICommand _applyAndCloseCommand;
        private ICommand _replaceGeneralSystemPromptCommand;
        private ICommand _replaceGenerateNLOSystemPromptCommand;
        private ICommand _replaceExtractNLOSystemPromptCommand;
        private ICommand _chooseModelCommand;
        private ICommand _upAgentCommand;
        private ICommand _downAgentCommand;
        private ICommand _cloneAgentCommand;

        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

        public AgentCollectionJson AgentCollection
        {
            get;
            private set;
        }

        public ObservableCollection2<AgentJson> AvailableAgents
        {
            get;
        }

        public AgentJson SelectedAgent
        {
            get => _selectedAgent;
            set
            {
                _selectedAgent = value;
                OnPropertyChanged();
            }
        }


        public Visibility ShowAgentPanel
        {
            get
            {
                if (_selectedAgent is null)
                {
                    return Visibility.Hidden;
                }

                return Visibility.Visible;
            }
        }


        public ICommand AddNewAgentCommand
        {
            get
            {
                if (_addNewAgentCommand is null)
                {
                    _addNewAgentCommand = new RelayCommand(
                        a =>
                        {
                            var newAgent = new AgentJson
                            {
                                Name = DateTime.Now.ToString("ddMMyyyy HH:mm:ss")
                            };
                            AgentCollection.Agents.Add(newAgent);
                            AvailableAgents.Add(newAgent);
                        });
                }

                return _addNewAgentCommand;
            }
        }

        public ICommand DeleteAgentCommand
        {
            get
            {
                if (_deleteAgentCommand is null)
                {
                    _deleteAgentCommand = new RelayCommand(
                        a =>
                        {
                            AgentCollection.Agents.Remove(_selectedAgent);
                            AvailableAgents.Remove(_selectedAgent);
                        },
                        a =>
                        {
                            if (_selectedAgent is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _deleteAgentCommand;
            }
        }

        public ICommand UpAgentCommand
        {
            get
            {
                if (_upAgentCommand is null)
                {
                    _upAgentCommand = new RelayCommand(
                        a =>
                        {
                            var index = AgentCollection.Agents.IndexOf(_selectedAgent);
                            AgentCollection.Agents.RemoveAt(index);
                            AgentCollection.Agents.Insert(index - 1, _selectedAgent);
                            _selectedAgent = null;

                            var aa = AvailableAgents[index];
                            AvailableAgents.RemoveAt(index);
                            AvailableAgents.Insert(index - 1, aa);

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_selectedAgent is null)
                            {
                                return false;
                            }

                            var index = AgentCollection.Agents.IndexOf(_selectedAgent);
                            if (index <= 0)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _upAgentCommand;
            }
        }

        public ICommand DownAgentCommand
        {
            get
            {
                if (_downAgentCommand is null)
                {
                    _downAgentCommand = new RelayCommand(
                        a =>
                        {
                            var index = AgentCollection.Agents.IndexOf(_selectedAgent);
                            AgentCollection.Agents.RemoveAt(index);
                            AgentCollection.Agents.Insert(index + 1, _selectedAgent);
                            _selectedAgent = null;

                            var aa = AvailableAgents[index];
                            AvailableAgents.RemoveAt(index);
                            AvailableAgents.Insert(index + 1, aa);

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_selectedAgent is null)
                            {
                                return false;
                            }

                            var index = AgentCollection.Agents.IndexOf(_selectedAgent);
                            if (index >= AgentCollection.Agents.Count - 1)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _downAgentCommand;
            }
        }

        public ICommand CloneAgentCommand
        {
            get
            {
                if (_cloneAgentCommand is null)
                {
                    _cloneAgentCommand = new RelayCommand(
                        a =>
                        {
                            var clone = (AgentJson)_selectedAgent.Clone();
                            clone.Name += FreeAIr.Resources.Resources.cloned;
                            AgentCollection.Agents.Add(clone);
                            AvailableAgents.Add(clone);

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (_selectedAgent is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _cloneAgentCommand;
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

        public ICommand ChooseModelCommand
        {
            get
            {
                if (_chooseModelCommand is null)
                {
                    _chooseModelCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            var chosenModelId = await ModelContextMenu.ChooseModelFromProviderAsync(
                                token: SelectedAgent.Technical.GetToken(),
                                endpoint: SelectedAgent.Technical.Endpoint,
                                title: FreeAIr.Resources.Resources.Choose_model_from_this_api_endpoint,
                                null
                                );
                            if (string.IsNullOrEmpty(chosenModelId))
                            {
                                return;
                            }

                            SelectedAgent.Technical.ChosenModel = chosenModelId;

                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (SelectedAgent is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _chooseModelCommand;
            }
        }

        public ICommand ReplaceGeneralSystemPromptCommand
        {
            get
            {
                if (_replaceGeneralSystemPromptCommand is null)
                {
                    _replaceGeneralSystemPromptCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            SelectedAgent.SystemPrompt = AgentCollectionJson.DefaultSystemPrompt;
                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (SelectedAgent is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _replaceGeneralSystemPromptCommand;
            }
        }

        public ICommand ReplaceGenerateNLOSystemPromptCommand
        {
            get
            {
                if (_replaceGenerateNLOSystemPromptCommand is null)
                {
                    _replaceGenerateNLOSystemPromptCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            SelectedAgent.SystemPrompt = AgentCollectionJson.CreateNewOutlinesSystemPrompt;
                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (SelectedAgent is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _replaceGenerateNLOSystemPromptCommand;
            }
        }

        public ICommand ReplaceExtractNLOSystemPromptCommand
        {
            get
            {
                if (_replaceExtractNLOSystemPromptCommand is null)
                {
                    _replaceExtractNLOSystemPromptCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            SelectedAgent.SystemPrompt = AgentCollectionJson.ExtractFileOutlinesSystemPrompt;
                            OnPropertyChanged();
                        },
                        a =>
                        {
                            if (SelectedAgent is null)
                            {
                                return false;
                            }

                            return true;
                        });
                }

                return _replaceExtractNLOSystemPromptCommand;
            }
        }

        public AgentConfigureViewModel(
            AgentCollectionJson agentCollection
            )
        {
            if (agentCollection is null)
            {
                throw new ArgumentNullException(nameof(agentCollection));
            }

            AgentCollection = agentCollection;
            AvailableAgents = new ObservableCollection2<AgentJson>(agentCollection.Agents);
        }
    }
}
