using FreeAIr.MCP.McpServerProxy;
using FreeAIr.UI.NestedCheckBox;
using FreeAIr.UI.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    public sealed class AvailableToolsViewModel : NestedCheckBoxViewModel
    {
        private readonly AvailableToolContainer _toolContainer;

        private ICommand _saveCommand;

        public string Header => "Choose the MCP tools you want to provide to LLM:";

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
                            foreach (var group in Groups)
                            {
                                var toolGroupName = group.Name;

                                _toolContainer.DeleteServer(
                                    toolGroupName
                                    );
                                _toolContainer.AddServer(
                                    toolGroupName
                                    );

                                foreach (var tool in group.Children)
                                {
                                    if (!(tool.Tag is McpServerToolStatus serverTool))
                                    {
                                        continue;
                                    }

                                    _toolContainer.AddTool(
                                        toolGroupName,
                                        tool.Name,
                                        tool.IsChecked
                                        );
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

        public AvailableToolsViewModel(
            AvailableToolContainer toolContainer
            )
        {
            if (toolContainer is null)
            {
                throw new ArgumentNullException(nameof(toolContainer));
            }

            Groups = new ObservableCollection2<CheckableItem>();

            _toolContainer = toolContainer;

            var tools = McpServerProxyCollection.GetTools(_toolContainer);

            var groups = new Dictionary<string, CheckableItem>();
            foreach (var toolsStatus in tools.ToolsStatuses)
            {
                groups[toolsStatus.McpServerProxyName] = new CheckableItem(
                    toolsStatus.McpServerProxyName,
                    string.Empty,
                    CheckableItemStyle.Empty,
                    null,
                    toolsStatus.Tools.Select(t => new CheckableItem(t.Tool.ToolName, t.Tool.Description, t.Enabled, CheckableItemStyle.Empty, t)).ToList()
                    );
            }
            Groups.AddRange(groups.Values);
        }
    }
}
