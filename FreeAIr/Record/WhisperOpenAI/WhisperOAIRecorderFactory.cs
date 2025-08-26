using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeAIr.Record.WhisperOpenAI
{
    [Export(typeof(IRecorderFactory))]
    public sealed class WhisperOAIRecorderFactory : IRecorderFactory
    {
        public const string RecorderName = "Whisper OpenAI API (including local LLMs)";

        public string Name => RecorderName;

        public UserControl CreateConfigurationControl()
        {
            return new WhisperOAIUserControl();
        }

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
