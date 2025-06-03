using FreeAIr.Antlr.Context;
using FreeAIr.Antlr.Prompt;
using FreeAIr.UI.Embedillo.VisualLine.Command;
using FreeAIr.UI.Embedillo.VisualLine.SolutionItem;
using FreeAIr.UI.ViewModels;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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

            SetupPromptControl();

            SetupAddToContextControl();

            viewModel.ContextControlFocus += ViewModel_ContextControlFocus;
            viewModel.PromptControlFocus += ViewModel_PromptControlFocus;
        }

        private void ViewModel_ContextControlFocus()
        {
            FocusContextControl();
        }

        private void ViewModel_PromptControlFocus()
        {
            FocusPromptControl();
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
                        FocusPromptControl();
                    }
                }
            }
            catch (Exception excp)
            {
                //todo log
            }
        }

        private void FocusContextControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                AddToContextControl.MakeFocused();
            });

            //await Task.Delay(100);
            //AddToContextControl.MakeFocused();
        }

        private void FocusPromptControl()
        {
            _ = Dispatcher.BeginInvoke(() =>
            {
                PromptControl.MakeFocused();
            });

            //await Task.Delay(100);
            //PromptControl.MakeFocused();
        }

        private void SetupAddToContextControl()
        {
            AddToContextControl.Setup(
                new ContextParser(
                    new SolutionItemVisualLineGeneratorFactory()
                    )
                );
        }

        private void SetupPromptControl()
        {
            PromptControl.Setup(
                new PromptParser(
                    new SolutionItemVisualLineGeneratorFactory(),
                    new CommandVisualLineGeneratorFactory()
                    )
                );
        }

    }

    public class RelativeMaxHeightConverter : IValueConverter
    {
        public double Ratio { get; set; } = 0.2; // 20% от высоты контейнера по умолчанию

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                return height * Ratio; // Возвращаем относительную высоту
            }
            return double.NaN;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

