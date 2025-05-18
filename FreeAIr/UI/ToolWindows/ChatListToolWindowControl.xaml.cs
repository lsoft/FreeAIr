using FreeAIr.Antlr.Context;
using FreeAIr.Antlr.Prompt;
using FreeAIr.Helper;
using FreeAIr.UI.Embedillo;
using FreeAIr.UI.Embedillo.VisualLine.Command;
using FreeAIr.UI.Embedillo.VisualLine.SolutionItem;
using FreeAIr.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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

            PromptControl.Setup(
                new PromptParser(
                    new SolutionItemVisualLineGeneratorFactory(),
                    new CommandVisualLineGeneratorFactory()
                    )
                );

            AddToContextControl.Setup(
                new ContextParser(
                    new SolutionItemVisualLineGeneratorFactory()
                    )
                );

            viewModel.MarkdownReReadEvent += ViewModel_MarkdownReReadEvent;
        }

        private void ViewModel_MarkdownReReadEvent(object sender, EventArgs e)
        {
            AnswerControl.ScrollToEnd();
        }

        private void ChatListToolWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (e.NewValue is bool)
                {
                    var visible = (bool)e.NewValue;
                    if (visible)
                    {
                        _ = Dispatcher.BeginInvoke(() =>
                        {
                            PromptControl.MakeFocused();
                        });

                        //await Task.Delay(100);
                        //PromptControl.MakeFocused();
                    }
                }
            }
            catch (Exception excp)
            {
                //todo log
            }
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

