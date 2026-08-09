using FreeAIr.UI.ViewModels;
using System.Windows.Controls;

namespace FreeAIr.UI.ToolWindows
{
    /// <summary>
    /// Code-behind for the natural language outline tree view; wires the supplied view model as
    /// the control's data context for the XAML bindings.
    /// </summary>
    public partial class NaturalLanguageOutlinesToolWindowControl : UserControl
    {
        /// <summary>
        /// Creates the control and binds it to the outline tree view model.
        /// </summary>
        public NaturalLanguageOutlinesToolWindowControl(
            NaturalLanguageOutlinesViewModel viewModel
            )
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            InitializeComponent();

            this.DataContext = viewModel;
        }
    }
}
