using FreeAIr.UI.ViewModels;
using System.Windows.Controls;

namespace FreeAIr.UI.ToolWindows
{
    /// <summary>
    /// Code-behind for the JSON export tool window's WPF view; wires the supplied view model as
    /// the control's data context so the XAML bindings for the natural language outline export
    /// settings resolve.
    /// </summary>
    public partial class BuildNaturalLanguageOutlinesJsonFileToolWindowControl : UserControl
    {
        /// <summary>
        /// Creates the control and binds it to the view model driving the JSON export.
        /// </summary>
        public BuildNaturalLanguageOutlinesJsonFileToolWindowControl(
            BuildNaturalLanguageOutlinesJsonFileToolViewModel viewModel
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
