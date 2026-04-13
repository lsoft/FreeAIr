using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class AgentConfigureViewModel : BaseViewModel
    {
        private AgentJson _selectedAgent;
        private readonly SupportCollectionJson _actionCollection;

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
                if (field is null)
                {
                    field = new RelayCommand(
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

                return field;
            }
        }

        public ICommand DeleteAgentCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
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

                return field;
            }
        }

        public ICommand UpAgentCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
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

                return field;
            }
        }

        public ICommand DownAgentCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
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

                return field;
            }
        }

        public ICommand CloneAgentCommand
        {
            get
            {
                if (field is null)
                {
                    field = new RelayCommand(
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

                return field;
            }
        }

        public ICommand ApplyAndCloseCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            var actions = _actionCollection.Actions.FindAll(ac =>
                                AgentCollection.Agents.All(ag => !string.IsNullOrEmpty(ac.AgentName) && ag.Name != ac.AgentName)
                                );

                            if (actions.Count > 0)
                            {
                                var actionNames = string.Join(",", actions.Select(ac => ac.Name));

                                await VS.MessageBox.ShowErrorAsync(
                                    string.Format(Resources.Resources.There_are_support_actions___actionNames, actionNames),
                                    string.Empty
                                    );
                            }

                            if (CloseWindow is not null)
                            {
                                CloseWindow(true);
                            }
                        });
                }

                return field;
            }
        }

        public string ModelFilter
        {
            get => field;
            set
            {
                if (value != field)
                {
                    field = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsMaskRegex
        {
            get => field;
            set
            {
                if (value != field)
                {
                    field = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ChooseModelCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
                        async a =>
                        {
                            var chosenModelId = await ModelContextMenu.ChooseModelFromProviderAsync(
                                token: SelectedAgent.Technical.GetToken(),
                                endpoint: SelectedAgent.Technical.Endpoint,
                                title: FreeAIr.Resources.Resources.Choose_model_from_this_api_endpoint,
                                null, ModelFilter, IsMaskRegex
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

                return field;
            }
        }

        public ICommand ReplaceGeneralSystemPromptCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        public ICommand ReplaceGenerateNLOSystemPromptCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        public ICommand ReplaceExtractNLOSystemPromptCommand
        {
            get
            {
                if (field is null)
                {
                    field = new AsyncRelayCommand(
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

                return field;
            }
        }

        public AgentConfigureViewModel(
            AgentCollectionJson agentCollection,
            SupportCollectionJson actionCollection
            )
        {
            if (agentCollection is null)
            {
                throw new ArgumentNullException(nameof(agentCollection));
            }

            if (actionCollection is null)
            {
                throw new ArgumentNullException(nameof(actionCollection));
            }

            AgentCollection = agentCollection;
            _actionCollection = actionCollection;
            AvailableAgents = new ObservableCollection2<AgentJson>(agentCollection.Agents);
        }
    }
}
