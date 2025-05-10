using FreeAIr.Helper;
using FreeAIr.UI.Embedillo.VisualLine.Command;
using FreeAIr.UI.Embedillo.VisualLine.SolutionItem;
using FreeAIr.UI.ViewModels;
using System.Windows;
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

            PromptControl.AddVisualLineGeneratorFactory(
                new SolutionItemVisualLineGeneratorFactory(),
                new CommandVisualLineGeneratorFactory()
                );

            AddToContextControl.AddVisualLineGeneratorFactory(
                new SolutionItemVisualLineGeneratorFactory()
                );

            viewModel.MarkdownReReadEvent += ViewModel_MarkdownReReadEvent;
        }

        private void ViewModel_MarkdownReReadEvent(object sender, EventArgs e)
        {
            AnswerControl.ScrollToEnd();
        }

    }

    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
            "Data",
            typeof(object),
            typeof(BindingProxy),
            new UIPropertyMetadata(null)
            );

        public object Data
        {
            get
            {
                return GetValue(DataProperty);
            }
            set
            {
                SetValue(DataProperty, value);
            }
        }
    }
}

