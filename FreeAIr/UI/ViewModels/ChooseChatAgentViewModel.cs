using FreeAIr.Options2.Agent;
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
        private ICommand _saveCommand;

        public string Header => FreeAIr.Resources.Resources.Choose_the_available_agent;

        public ObservableCollection2<CheckableItem> Groups
        {
            get;
            set;
        }

        public AgentJson? ChosenAgent
        {
            get;
            private set;
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
                                var optionAgent = agentCheckableItem.Tag as AgentJson;
                                if (agentCheckableItem.IsChecked.GetValueOrDefault(false))
                                {
                                    ChosenAgent = optionAgent;
                                    break;
                                }
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
            AgentCollectionJson chatAgents,
            AgentJson? chosenAgent
            )
        {
            Groups = new ObservableCollection2<CheckableItem>();

            var agents = chatAgents.Agents.FindAll(a => a.Technical.HasToken());

            ChosenAgent = chosenAgent;
            Groups.Add(
                new SingleCheckedCheckableItem(
                    FreeAIr.Resources.Resources.Available_agents,
                    "",
                    CheckableItemStyle.Empty,
                    null,
                    agents.Select(agent =>
                        new CheckableItem(
                            agent.Name,
                            string.Empty,
                            agent.Name == ChosenAgent?.Name,
                            CheckableItemStyle.Empty,
                            agent
                            )
                        ).ToList()
                    )
                );
        }
    }
}
