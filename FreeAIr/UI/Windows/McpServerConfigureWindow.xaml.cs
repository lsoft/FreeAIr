using FreeAIr.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI.Windows
{
    public partial class McpServerConfigureWindow : Window
    {
        public McpServerConfigureWindow(
            )
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as McpServerConfigureViewModel;
            if (vm is not null)
            {
                vm.CloseWindow = result =>
                {
                    DialogResult = result;
                };
            }
        }
    }

}
