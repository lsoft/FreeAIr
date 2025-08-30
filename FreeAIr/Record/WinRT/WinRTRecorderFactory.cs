using FreeAIr.Record.WinRT;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeAIr.Record.WinRT
{
    [Export(typeof(IRecorderFactory))]
    public sealed class WinRTRecorderFactory : IRecorderFactory
    {
        public const string RecorderName = "WinRT recorder and transcriber";

        public string Name => RecorderName;

        public UserControl? CreateConfigurationControl()
        {
            return null;
        }

        public async Task<IRecorder> CreateRecorderAsync()
        {
            var recorder =  new WinRTRecorder(
                );

            await recorder.InitAsync();

            return recorder;
        }
    }
}
