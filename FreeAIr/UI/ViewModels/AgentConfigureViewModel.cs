using FreeAIr.Options2;
using FreeAIr.Options2.Agent;
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
        private AgentJson _selectedAgent;
        private ICommand _addNewAgentCommand;
        private ICommand _deleteAgentCommand;
        private ICommand _applyAndCloseCommand;

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
