using System.Windows.Controls;

namespace FreeAIr.UI.CodeLens
{
    /// <summary>
    /// Interaction logic for CodeLenseUserControl.xaml
    /// </summary>
    public partial class CodeLenseUserControl : UserControl
    {
        /// <summary>
        /// Creates the CodeLens popup control that renders FreeAIr's inline actions (e.g. explain,
        /// ask about) above a method or class in the editor.
        /// </summary>
        public CodeLenseUserControl()
        {
            InitializeComponent();
        }
    }
}
