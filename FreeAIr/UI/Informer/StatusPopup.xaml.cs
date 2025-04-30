using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
