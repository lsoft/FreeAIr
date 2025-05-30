using FreeAIr.MCP.Agent;
using FreeAIr.UI.NestedCheckBox;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class AvailableToolsViewModel : BaseViewModel
    {
        private ICommand _saveToolsCommand;

        public ObservableCollection2<CheckableGroup> Groups
        {
            get;
            set;
        }

        public Action? CloseWindow
        {
            get;
            set;
        }

        public ICommand SaveToolsCommand
        {
            get
            {
                if (_saveToolsCommand == null)
                {
                    _saveToolsCommand = new RelayCommand(
                        a =>
                        {
                            foreach (var group in Groups)
                            {
                                var toolGroupName = group.Name;

                                foreach (var tool in group.Children)
                                {
                                    if (!(tool.Tag is AgentToolStatus agentTool))
                                    {
                                        continue;
                                    }
                                    if (!tool.HasChanged)
                                    {
                                        continue;
                                    }

                                    AvailableToolController.UpdateTool(
                                        toolGroupName,
                                        tool.Name,
                                        tool.IsChecked
                                        );
                                }
                            }

                            if (CloseWindow is not null)
                            {
                                CloseWindow();
                            }
                        });
                }

                return _saveToolsCommand;
            }
        }

        public AvailableToolsViewModel()
        {
            Groups = new ObservableCollection2<CheckableGroup>();

            var tools = AgentCollection.GetTools();

            var groups = new Dictionary<string, CheckableGroup>();
            foreach (var agent in tools.Agents)
            {
                groups[agent.AgentName] = new CheckableGroup(
                    agent.AgentName,
                    string.Empty,
                    null,
                    agent.Tools.Select(t => new CheckableItem(t.Tool.ToolName, t.Tool.Description, t.Enabled, t)).ToList()
                    );
            }
            Groups.AddRange(groups.Values);
        }
    }
}
