using FreeAIr.Helper;
using FreeAIr.UI.ViewModels;
using System.Windows;

namespace FreeAIr.UI.Wizard
{
    /// <summary>
    /// The modal setup wizard window. It only hosts the panels and hands the view model a way to
    /// close itself - everything the wizard actually does lives in <see cref="SetupWizardViewModel"/>.
    /// </summary>
    public partial class SetupWizardWindow : Window
    {
        public SetupWizardWindow(
            )
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //a throw out of a Loaded handler reaches WPF's dispatcher unhandled, which inside
            //devenv ends the Visual Studio process rather than the dialog
            try
            {
                var vm = this.DataContext as SetupWizardViewModel;
                if (vm is not null)
                {
                    vm.CloseWindow = result =>
                    {
                        DialogResult = result;
                    };
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }
    }
}
