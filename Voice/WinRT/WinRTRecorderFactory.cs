using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeAIr.Record.WinRT
{
    /// <summary>Discovered by MEF as an <see cref="IRecorderFactory"/>; builds <see cref="WinRTRecorder"/>, the Windows Runtime speech recognition backend.</summary>
    [Export(typeof(IRecorderFactory))]
    public sealed class WinRTRecorderFactory : IRecorderFactory
    {
        /// <summary>Display name of this backend, shown in the recorder picker.</summary>
        public const string RecorderName = "WinRT recorder and transcriber";

        /// <inheritdoc/>
        public string Name => RecorderName;

        /// <summary>None needed — the WinRT speech engine requires no configuration.</summary>
        public UserControl? CreateConfigurationControl()
        {
            return null;
        }

        /// <summary>Builds and initializes a <see cref="WinRTRecorder"/>.</summary>
        public async Task<IRecorder> CreateRecorderAsync()
        {
            var recorder =  new WinRTRecorder(
                );

            await recorder.InitAsync();

            return recorder;
        }
    }
}
