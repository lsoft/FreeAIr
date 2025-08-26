using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace FreeAIr.Record.WhisperNet
{
    /// <summary>
    /// Interaction logic for WhisperNetUserControl.xaml
    /// </summary>
    public partial class WhisperNetUserControl : UserControl
    {
        public WhisperNetUserControl()
        {
            InitializeComponent();

            ModelFilePathTextBox.Text = RecordingPage.Instance.WhisperNet_ModelFilePath;
            PromptTextBox.Text = RecordingPage.Instance.WhisperNet_Prompt;
        }

        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://huggingface.co/sandrohanea/whisper.net/tree/main");
        }

        private void ModelFilePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperNet_ModelFilePath = ModelFilePathTextBox.Text;
            RecordingPage.Instance.Save();
        }

        private void PromptTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperNet_Prompt = PromptTextBox.Text;
            RecordingPage.Instance.Save();
        }
    }
}
