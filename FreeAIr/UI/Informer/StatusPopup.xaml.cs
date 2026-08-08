using System.Windows.Controls;

namespace FreeAIr.UI.Informer
{
    /// <summary>
    /// Interaction logic for StatusPopup.xaml
    /// </summary>
    public partial class StatusPopup : UserControl
    {
        /// <summary>
        /// Creates an empty status popup; call <see cref="SetText"/> to set its message.
        /// </summary>
        public StatusPopup()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates a status popup preloaded with the given message text.
        /// </summary>
        public StatusPopup(
            string text
            )
        {
            InitializeComponent();

            SetText(text);
        }

        /// <summary>
        /// Sets the message text shown in the popup body.
        /// </summary>
        public void SetText(string text)
        {
            PopupText.Text = text;
        }
    }
}
