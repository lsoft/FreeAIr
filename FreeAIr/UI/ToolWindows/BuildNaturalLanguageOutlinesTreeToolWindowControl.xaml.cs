using FreeAIr.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI.ToolWindows
{
    public partial class BuildNaturalLanguageOutlinesJsonFileToolWindowControl : UserControl
    {
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
