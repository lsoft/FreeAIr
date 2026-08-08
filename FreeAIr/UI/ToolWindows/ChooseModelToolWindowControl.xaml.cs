using FreeAIr.UI.ViewModels;
using System.Windows.Controls;

namespace FreeAIr.UI.ToolWindows
{
    /// <summary>
    /// Code-behind for the OpenRouter model picker tool window; simply wires the supplied view
    /// model as the control's data context for the XAML bindings.
    /// </summary>
    public partial class ChooseModelToolWindowControl : UserControl
    {
        /// <summary>
        /// Creates the control and binds it to the model picker view model.
        /// </summary>
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
