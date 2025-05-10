using System.Windows.Controls;

namespace FreeAIr.UI.Informer
{
    /// <summary>
    /// Interaction logic for StatusPopup.xaml
    /// </summary>
    public partial class StatusPopup : UserControl
    {
        public StatusPopup()
        {
            InitializeComponent();
        }

        public StatusPopup(
            string text
            )
        {
            InitializeComponent();

            SetText(text);
        }

        public void SetText(string text)
        {
            PopupText.Text = text;
        }
    }
}
