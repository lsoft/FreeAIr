using FreeAIr.UI.ViewModels;
using System.Windows.Controls;

namespace FreeAIr.UI.ToolWindows
{
    public partial class ChooseModelToolWindowControl : UserControl
    {
        public ChooseModelToolWindowControl(
            ChooseModelViewModel viewModel
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
