using FreeAIr.UI.ViewModels;
using System.Windows;

namespace FreeAIr.UI.Windows
{
    public partial class ActionConfigureWindow : Window
    {
        public ActionConfigureWindow(
            )
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as ActionConfigureViewModel;
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
