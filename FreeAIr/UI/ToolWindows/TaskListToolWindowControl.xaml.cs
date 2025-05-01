using FreeAIr.UI.ViewModels;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using MdXaml.Plugins;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace FreeAIr.UI.ToolWindows
{
    public partial class TaskListToolWindowControl : UserControl
    {
        public TaskListToolWindowControl(
            TaskListViewModel viewModel
            )
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            DataContext = viewModel;

            InitializeComponent();
        }

    }
}
