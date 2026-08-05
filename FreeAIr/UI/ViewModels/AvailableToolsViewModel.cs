using FreeAIr.MCP.McpServerProxy;
using FreeAIr.UI.NestedCheckBox;
using FreeAIr.UI.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.ViewModels
{
    /// <summary>
    /// Backs the nested-checkbox tool picker that lets the user enable or disable individual MCP
    /// server tools, grouped by the MCP server (proxy) that exposes them.
    /// </summary>
    public sealed class AvailableToolsViewModel : NestedCheckBoxViewModel
    {
        private readonly AvailableToolContainer _toolContainer;

        /// <summary>
        /// The heading text shown above the tool checkbox tree.
        /// </summary>
        public string Header => FreeAIr.Resources.Resources.Choose_the_MCP_tools_you_want_to;

        /// <summary>
        /// One top-level checkable group per MCP server, each containing its tools as children.
        /// </summary>
        public ObservableCollection2<CheckableItem> Groups
        {
            get;
            set;
        }

        /// <summary>
        /// Persists the checked/unchecked state of every tool back into the tool container, then
        /// closes the picker.
        /// </summary>
        public ICommand SaveCommand
        {
            get
            {
                if (field == null)
                {
                    field = new RelayCommand(
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
                                        tool.IsChecked.GetValueOrDefault(false)
                                        );
                                }
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
        /// Builds the tool picker's checkbox tree from the current set of MCP servers and tools
        /// in <paramref name="toolContainer"/>.
        /// </summary>
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
