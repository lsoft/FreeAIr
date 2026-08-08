using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeAIr.Record.WhisperOpenAI
{
    /// <summary>Discovered by MEF as an <see cref="IRecorderFactory"/>; builds <see cref="WhisperOAIRecorder"/>, which transcribes through any OpenAI-compatible audio endpoint.</summary>
    [Export(typeof(IRecorderFactory))]
    public sealed class WhisperOAIRecorderFactory : IRecorderFactory
    {
        /// <summary>Display name of this backend, shown in the recorder picker.</summary>
        public const string RecorderName = "Whisper OpenAI API (including local LLMs)";

        /// <inheritdoc/>
        public string Name => RecorderName;

        /// <summary>The control where the user enters the model name, token, endpoint and prompt.</summary>
        public UserControl CreateConfigurationControl()
        {
            return new WhisperOAIUserControl();
        }

        /// <summary>Builds and initializes a <see cref="WhisperOAIRecorder"/> from the endpoint settings saved in the recording page.</summary>
        public async Task<IRecorder> CreateRecorderAsync()
        {
            var recorder =  new WhisperOAIRecorder(
                modelName: RecordingPage.Instance.WhisperOAI_ModelName,
                token: RecordingPage.Instance.WhisperOAI_Token,
                endpoint: RecordingPage.Instance.WhisperOAI_Endpoint,
                prompt: RecordingPage.Instance.WhisperOAI_Prompt
                );

            await recorder.InitAsync();

            return recorder;
        }
    }
}
