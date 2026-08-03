using FreeAIr.UI.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;

namespace FreeAIr.UI.ToolWindows
{
    public partial class RagCalibrationToolWindowControl : UserControl
    {
        public RagCalibrationToolWindowControl(
            RagCalibrationViewModel viewModel
            )
        {
            if (viewModel is null)
            {
                throw new ArgumentNullException(nameof(viewModel));
            }

            InitializeComponent();

            this.DataContext = viewModel;
        }

        /// <summary>
        /// Makes Enter mean `I have finished typing this` in every text box of the window.
        ///
        /// The boxes of the two lists commit on losing focus, which is right while the user is
        /// tabbing through them and wrong the moment they type a query and press Enter: the value
        /// would still be sitting in the box, unseen by the measurement. Doing it here rather than
        /// per box keeps the rule in one place, and it runs before the Enter of the query box
        /// reaches its command.
        /// </summary>
        private void OnPreviewKeyDown(
            object sender,
            KeyEventArgs e
            )
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            if (Keyboard.FocusedElement is not TextBox box)
            {
                return;
            }

            box.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }
    }
}
