using FreeAIr.UI.ViewModels;
using System.Windows.Controls;

namespace FreeAIr.UI.ToolWindows
{
    public partial class NaturalLanguageOutlinesToolWindowControl : UserControl
    {
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
