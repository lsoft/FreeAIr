using FreeAIr.UI.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace FreeAIr.UI.Windows
{
    public partial class AgentConfigureWindow : Window
    {
        public AgentConfigureWindow(
            )
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as AgentConfigureViewModel;
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
