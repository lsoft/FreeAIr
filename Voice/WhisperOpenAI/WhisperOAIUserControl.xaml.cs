using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace FreeAIr.Record.WhisperOpenAI
{
    /// <summary>
    /// The settings control for the OpenAI-compatible Whisper recorder: model name, token, endpoint
    /// and decoding prompt, shown before switching to <see cref="WhisperOAIRecorder"/>.
    /// </summary>
    public partial class WhisperOAIUserControl : UserControl
    {
        /// <summary>Loads the endpoint settings already saved in the recording settings into the text boxes.</summary>
        public WhisperOAIUserControl()
        {
            InitializeComponent();

            ModelNameTextBox.Text = RecordingPage.Instance.WhisperOAI_ModelName;
            TokenTextBox.Text = RecordingPage.Instance.WhisperOAI_Token;
            EndpointTextBox.Text = RecordingPage.Instance.WhisperOAI_Endpoint;
            PromptTextBox.Text = RecordingPage.Instance.WhisperOAI_Prompt;
        }

        /// <summary>Opens the OpenAI Whisper model page in the default browser.</summary>
        private void Label_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://huggingface.co/collections/openai/whisper-release-6501bba2cf999715fd953013");
        }

        /// <summary>Persists the model name into the recording settings as it is typed.</summary>
        private void ModelNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperOAI_ModelName = ModelNameTextBox.Text;
            RecordingPage.Instance.Save();
        }

        /// <summary>Persists the access token into the recording settings as it is typed.</summary>
        private void TokenTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperOAI_Token = TokenTextBox.Text;
            RecordingPage.Instance.Save();
        }

        /// <summary>Persists the endpoint url into the recording settings as it is typed.</summary>
        private void EndpointTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperOAI_Endpoint = EndpointTextBox.Text;
            RecordingPage.Instance.Save();
        }

        /// <summary>Persists the decoding prompt into the recording settings as it is typed.</summary>
        private void PromptTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RecordingPage.Instance.WhisperOAI_Prompt = PromptTextBox.Text;
            RecordingPage.Instance.Save();
        }

    }
}
