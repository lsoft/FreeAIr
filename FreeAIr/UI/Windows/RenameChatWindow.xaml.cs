using FreeAIr.UI.ViewModels;
using System;
using System.Windows;

namespace FreeAIr.UI.Windows
{
    public partial class RenameChatWindow : Window
    {
        public RenameChatWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as RenameChatViewModel;
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
