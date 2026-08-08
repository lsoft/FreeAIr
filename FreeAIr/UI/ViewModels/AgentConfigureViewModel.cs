using FreeAIr.Options2.Agent;
using FreeAIr.Options2.Support;
using FreeAIr.UI.ContextMenu;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// Backs the Configure Agents window, where the user creates, reorders, clones and deletes the
    /// named AI agent profiles (model endpoint, chosen model, system prompt) used elsewhere in FreeAIr.
    /// </summary>
    public sealed class AgentConfigureViewModel : BaseViewModel
    {
        /// <summary>
        /// The agent currently highlighted in the agents list, whose settings are shown in the detail panel.
        /// </summary>
        private AgentJson _selectedAgent;
        /// <summary>
        /// The support/context-menu actions collection, used to warn the user when closing the window
        /// would leave an action referencing an agent name that no longer exists.
        /// </summary>
        private readonly SupportCollectionJson _actionCollection;

        /// <summary>
        /// Callback invoked to close the Configure Agents window, passing whether the changes should be applied.
        /// </summary>
        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

        /// <summary>
        /// The full set of configured agent profiles as persisted in settings, backing the agents list
        /// shown in the Configure Agents window.
        /// </summary>
        public AgentCollectionJson AgentCollection
        {
            get;
            private set;
        }

        /// <summary>
        /// The observable projection of <see cref="AgentCollection"/>'s agents bound to the agents list
        /// box in the Configure Agents window.
        /// </summary>
        public ObservableCollection2<AgentJson> AvailableAgents
        {
            get;
        }

        /// <summary>
        /// The agent profile currently selected in the agents list; drives which agent's settings the
        /// detail panel edits.
        /// </summary>
        public AgentJson SelectedAgent
        {
            get => _selectedAgent;
            set
            {
                _selectedAgent = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// Whether the agent detail panel should be visible; hidden while no agent is selected in the list.
        /// </summary>
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


        /// <summary>
        /// Creates a new agent profile named with the current timestamp and adds it to both the
        /// persisted collection and the list shown in the Configure Agents window.
        /// </summary>
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

        /// <summary>
        /// Removes the currently selected agent profile from the collection and the agents list;
        /// enabled only while an agent is selected.
        /// </summary>
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

        /// <summary>
        /// Moves the currently selected agent one position earlier in the agents list, changing the
        /// order agents are offered elsewhere in the UI; disabled when the selected agent is already first.
        /// </summary>
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

        /// <summary>
        /// Moves the currently selected agent one position later in the agents list, changing the
        /// order agents are offered elsewhere in the UI; disabled when the selected agent is already last.
        /// </summary>
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

        /// <summary>
        /// Duplicates the currently selected agent profile, appending a "cloned" marker to its name,
        /// and adds the copy to the collection and the agents list; enabled only while an agent is selected.
        /// </summary>
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

        /// <summary>
        /// Applies the agent configuration and closes the window; first warns the user if any support
        /// action still references an agent name that was removed or renamed.
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

        /// <summary>
        /// The text typed into the model filter box, used to narrow the list of models offered by
        /// <see cref="ChooseModelCommand"/> when picking a model for the selected agent's endpoint.
        /// </summary>
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

        /// <summary>
        /// Whether <see cref="ModelFilter"/> should be interpreted as a regular expression instead of
        /// a plain substring mask when filtering the model list for <see cref="ChooseModelCommand"/>.
        /// </summary>
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

        /// <summary>
        /// Opens the model chooser context menu against the selected agent's technical endpoint,
        /// filtered by <see cref="ModelFilter"/>/<see cref="IsMaskRegex"/>, and applies the chosen
        /// model id to the agent; enabled only while an agent is selected.
        /// </summary>
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
                                filterer: new ModelFilterer(
                                    ModelFilter,
                                    IsMaskRegex
                                    )
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

        /// <summary>
        /// Resets the selected agent's system prompt to the built-in default general-purpose prompt;
        /// enabled only while an agent is selected.
        /// </summary>
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

        /// <summary>
        /// Replaces the selected agent's system prompt with the built-in prompt used to generate
        /// natural-language outlines (NLO) for the RAG search index; enabled only while an agent is selected.
        /// </summary>
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

        /// <summary>
        /// Replaces the selected agent's system prompt with the built-in prompt used to extract
        /// natural-language outlines (NLO) from a file's existing content; enabled only while an agent is selected.
        /// </summary>
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

        /// <summary>
        /// Creates the view model for the Configure Agents window, wiring it to the persisted agent
        /// collection and to the support actions collection used to validate agent renames on close.
        /// </summary>
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
