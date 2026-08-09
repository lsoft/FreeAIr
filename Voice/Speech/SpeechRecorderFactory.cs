using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeAIr.Record.Speech
{
    /// <summary>Discovered by MEF as an <see cref="IRecorderFactory"/>; builds <see cref="SpeechRecorder"/>, the Windows Speech Recognition backend.</summary>
    [Export(typeof(IRecorderFactory))]
    public sealed class SpeechRecorderFactory : IRecorderFactory
    {
        /// <summary>Display name of this backend, shown in the recorder picker.</summary>
        public const string RecorderName = "Microsoft speech API recorder and transcriber";

        /// <inheritdoc/>
        public string Name => RecorderName;

        /// <summary>None needed — the Windows speech engine requires no configuration.</summary>
        public UserControl? CreateConfigurationControl()
        {
            return null;
        }

        /// <summary>Builds and initializes a <see cref="SpeechRecorder"/>.</summary>
        public async Task<IRecorder> CreateRecorderAsync()
        {
            var recorder =  new SpeechRecorder(
                );

            await recorder.InitAsync();

            return recorder;
        }
    }
}
