using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace FreeAIr.Record.WhisperNet
{
    /// <summary>
    /// Interaction logic for WhisperNetUserControl.xaml
    /// </summary>
    public partial class WhisperNetUserControl : UserControl
    {
        /// <summary>Loads the local model file path and the decoding prompt already saved in the recording settings into the two text boxes.</summary>
        public WhisperNetUserControl()
        {
            InitializeComponent();

            ModelFilePathTextBox.Text = RecordingPage.Instance.WhisperNet_ModelFilePath;
            PromptTextBox.Text = RecordingPage.Instance.WhisperNet_Prompt;
        }

        /// <summary>Opens the Whisper.Net model download page in the default browser.</summary>
        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://huggingface.co/sandrohanea/whisper.net/tree/main");
        }

        /// <summary>Persists the model file path into the recording settings as it is typed.</summary>
        private void ModelFilePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperNet_ModelFilePath = ModelFilePathTextBox.Text;
            RecordingPage.Instance.Save();
        }

        /// <summary>Persists the decoding prompt into the recording settings as it is typed.</summary>
        private void PromptTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperNet_Prompt = PromptTextBox.Text;
            RecordingPage.Instance.Save();
        }
    }
}
