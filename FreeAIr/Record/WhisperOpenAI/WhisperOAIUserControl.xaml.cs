using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace FreeAIr.Record.WhisperOpenAI
{
    public partial class WhisperOAIUserControl : UserControl
    {
        public WhisperOAIUserControl()
        {
            InitializeComponent();

            ModelNameTextBox.Text = RecordingPage.Instance.WhisperOAI_ModelName;
            TokenTextBox.Text = RecordingPage.Instance.WhisperOAI_Token;
            EndpointTextBox.Text = RecordingPage.Instance.WhisperOAI_Endpoint;
            PromptTextBox.Text = RecordingPage.Instance.WhisperOAI_Prompt;
        }

        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://huggingface.co/collections/openai/whisper-release-6501bba2cf999715fd953013");
        }

        private void ModelNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperOAI_ModelName = ModelNameTextBox.Text;
            RecordingPage.Instance.Save();
        }

        private void TokenTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperOAI_Token = TokenTextBox.Text;
            RecordingPage.Instance.Save();
        }

        private void EndpointTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperOAI_Endpoint = EndpointTextBox.Text;
            RecordingPage.Instance.Save();
        }

        private void PromptTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperOAI_Prompt = PromptTextBox.Text;
            RecordingPage.Instance.Save();
        }

    }
}
