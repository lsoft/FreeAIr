using FreeAIr.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

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
