using FreeAIr.UI.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace FreeAIr.UI.Windows
{
    public partial class NestedCheckBoxWindow : Window
    {
        public NestedCheckBoxWindow()
        {
            InitializeComponent();

            TitleBar.MouseLeftButtonDown += DragWindow;
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is AvailableToolsViewModel vm)
            {
                vm.CloseWindow = this.Close;
            }
        }
    }
}