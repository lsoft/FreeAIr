using FreeAIr.Agents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class AgentConfigureViewModel : BaseViewModel
    {
        private Agent _selectedAgent;
        private ICommand _addNewAgentCommand;
        private ICommand _deleteAgentCommand;
        private ICommand _saveAndCloseCommand;

        public AgentCollection AgentCollection
        {
            get;
            private set;
        }

        public ObservableCollection2<Agent> AvailableAgents
        {
            get;
        }

        public Agent SelectedAgent
        {
            get => _selectedAgent;
            set
            {
                _selectedAgent = value;
                OnPropertyChanged();
            }
        }


        public bool IsDefaultAgent
        {
            get => _selectedAgent?.IsDefault ?? false;
            set
            {
                if (_selectedAgent is null)
                {
                    return;
                }
                if (AgentCollection.Agents.Count == 0)
                {
                    return;
                }

                if (value)
                {
                    foreach (var agent in AgentCollection.Agents)
                    {
                        agent.IsDefault = ReferenceEquals(agent, _selectedAgent);
                    }
                }
                else
                {
                    foreach (var agent in AgentCollection.Agents)
                    {
                        agent.IsDefault = false;
                    }
                    var notSelected = AgentCollection.Agents.FirstOrDefault(a => !ReferenceEquals(a, _selectedAgent));
                    if (notSelected is not null)
                    {
                        notSelected.IsDefault = true;
                    }
                }

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
                            var newAgent = new Agent
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

        public ICommand SaveAndCloseCommand
        {
            get
            {
                if (_saveAndCloseCommand is null)
                {
                    _saveAndCloseCommand = new AsyncRelayCommand(
                        async a =>
                        {
                            await InternalPage.Instance.SaveAgentsAsync(AgentCollection);
                        });
                }

                return _saveAndCloseCommand;
            }
        }


        public AgentConfigureViewModel(
            AgentCollection agentCollection
            )
        {
            if (agentCollection is null)
            {
                throw new ArgumentNullException(nameof(agentCollection));
            }

            AgentCollection = agentCollection;
            AvailableAgents = new ObservableCollection2<Agent>(agentCollection.Agents);
        }
    }
}
