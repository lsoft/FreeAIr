using FreeAIr.Helper;
using FreeAIr.UI.ViewModels;
using System.Windows.Controls;

namespace FreeAIr.UI.ToolWindows
{
    public partial class ChatListToolWindowControl : UserControl
    {
        public ChatListToolWindowControl(
            ChatListViewModel viewModel
            )
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            DataContext = viewModel;

            InitializeComponent();

            viewModel.MarkdownReReadEvent += ViewModel_MarkdownReReadEvent;
        }

        private void ViewModel_MarkdownReReadEvent(object sender, EventArgs e)
        {
            AnswerControl.ScrollToEnd();
        }
    }

}

