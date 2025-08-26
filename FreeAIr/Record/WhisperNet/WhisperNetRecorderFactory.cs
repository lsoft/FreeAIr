using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeAIr.Record.WhisperNet
{
    [Export(typeof(IRecorderFactory))]
    public sealed class WhisperNetRecorderFactory : IRecorderFactory
    {
        public const string RuntimeZipFileName = "WhisperNet.Runtime.zip";

        private static readonly string _runtimeFolderPath;

        public const string RecorderName = "Whisper local (using Whisper.Net; processing on CPU)";

        public string Name => RecorderName;

        static WhisperNetRecorderFactory()
        {
            _runtimeFolderPath = Path.Combine(FreeAIrPackage.WorkingFolder, @"runtimes\win-x64");

            Unpack();
        }

        public UserControl CreateConfigurationControl()
        {
            return new WhisperNetUserControl();
        }

        public async Task<IRecorder> CreateRecorderAsync()
        {
            var modelFilePath = RecordingPage.Instance.WhisperNet_ModelFilePath;
            var prompt = RecordingPage.Instance.WhisperNet_Prompt;

            var recorder =  new WhisperNetRecorder(
                modelFilePath,
                prompt
                );

            await recorder.InitAsync();

            return recorder;
        }

        private static void Unpack()
        {
            if (!Directory.Exists(_runtimeFolderPath))
            {
                return;
            }

            var zipFilePath = Path.Combine(
                FreeAIrPackage.WorkingFolder,
                _runtimeFolderPath,
                RuntimeZipFileName
                );
            using var zip = ZipFile.OpenRead(zipFilePath);
            zip.ExtractToDirectory(_runtimeFolderPath);
        }
    }
}
