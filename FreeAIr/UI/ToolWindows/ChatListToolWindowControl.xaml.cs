using FreeAIr.Helper;
using FreeAIr.UI.Embedillo;
using FreeAIr.UI.Embedillo.VisualLine.Command;
using FreeAIr.UI.Embedillo.VisualLine.SourceFile;
using FreeAIr.UI.ViewModels;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

            PromptControl.AddVisualLineGeneratorFactory(
                new SourceFileVisualLineGeneratorFactory()
                );
            PromptControl.AddVisualLineGeneratorFactory(
                new CommandVisualLineGeneratorFactory()
                );

            AddToContextControl.AddVisualLineGeneratorFactory(
                new SourceFileVisualLineGeneratorFactory()
                );

            viewModel.MarkdownReReadEvent += ViewModel_MarkdownReReadEvent;
        }

        private void ViewModel_MarkdownReReadEvent(object sender, EventArgs e)
        {
            AnswerControl.ScrollToEnd();
        }
    }

}

