using FreeAIr.Agents;
using FreeAIr.MCP.McpServerProxy;
using FreeAIr.Shared.Helper;
using FreeAIr.UI.NestedCheckBox;
using FreeAIr.UI.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class ChooseChatAgentViewModel : NestedCheckBoxViewModel
    {
        private readonly AgentCollection _chatAgents;
        
        private ICommand _saveCommand;

        public string Header => "Choose the available agent:";

        public ObservableCollection2<CheckableItem> Groups
        {
            get;
            set;
        }

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(
                        a =>
                        {
                            var agentCheckableItems = Groups[0];
                            foreach (var agentCheckableItem in agentCheckableItems.Children)
                            {
                                var optionAgent = agentCheckableItem.Tag as Agent;
                                optionAgent.IsDefault = agentCheckableItem.IsChecked;
                            }

                            if (CloseWindow is not null)
                            {
                                CloseWindow(true);
                            }
                        });
                }

                return _saveCommand;
            }
        }

        public ChooseChatAgentViewModel(
            AgentCollection chatAgents
            )
        {
            Groups = new ObservableCollection2<CheckableItem>();

            _chatAgents = chatAgents;

            Groups.Add(
                new SingleCheckedCheckableItem(
                    "Available agents:",
                    "",
                    null,
                    chatAgents,
                    _chatAgents.Agents.Select(agent =>
                        new CheckableItem(
                            agent.Name,
                            string.Empty,
                            agent.IsDefault,
                            null,
                            agent
                            )
                        ).ToList()
                    )
                );
            _chatAgents = chatAgents;
        }
    }
}
