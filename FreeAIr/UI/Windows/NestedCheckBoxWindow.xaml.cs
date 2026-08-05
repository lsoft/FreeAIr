using System.Windows;
using System.Windows.Input;
using WpfHelpers;

namespace FreeAIr.UI.Windows
{
    /// <summary>
    /// Borderless dialog window that hosts a nested checkbox tree (used for choosing global
    /// tools and similar hierarchical selections), with a custom title bar drag handle and
    /// Escape-to-cancel behavior.
    /// </summary>
    public partial class NestedCheckBoxWindow : Window
    {
        /// <summary>
        /// Creates the window and wires the custom title bar to support dragging.
        /// </summary>
        public NestedCheckBoxWindow()
        {
            InitializeComponent();

            TitleBar.MouseLeftButtonDown += DragWindow;
        }

        /// <summary>
        /// Lets the user drag the borderless window by its custom title bar.
        /// </summary>
        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// Closes the dialog with a cancelled result when the user presses Escape.
        /// </summary>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }

        /// <summary>
        /// Closes the dialog with a cancelled result when the Cancel button is clicked.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// Wires the view model's <see cref="NestedCheckBoxViewModel.CloseWindow"/> callback to
        /// set this window's dialog result, so the view model can close the window it does not
        /// otherwise reference.
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is NestedCheckBoxViewModel vm)
            {
                vm.CloseWindow = result =>
                {
                    DialogResult = result;
                };
            }
        }
    }

    /// <summary>
    /// Base view model for a <see cref="NestedCheckBoxWindow"/> dialog, providing the hook the
    /// window uses to close itself with a result once the view model decides the dialog is done.
    /// </summary>
    public abstract class NestedCheckBoxViewModel : BaseViewModel
    {
        /// <summary>
        /// Callback set by the window that, when invoked, closes it with the given dialog result.
        /// </summary>
        public Action<bool>? CloseWindow
        {
            get;
            set;
        }

    }
}
