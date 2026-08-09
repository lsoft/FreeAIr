using FreeAIr.Helper;
using System.ComponentModel.Composition;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace FreeAIr.Record.WhisperNet
{
    /// <summary>
    /// Discovered by MEF as an <see cref="IRecorderFactory"/>; builds <see cref="WhisperNetRecorder"/>
    /// and unpacks the native Whisper.Net runtimes (embedded in the VSIX as a zip) on first use.
    /// </summary>
    [Export(typeof(IRecorderFactory))]
    public sealed class WhisperNetRecorderFactory : IRecorderFactory
    {
        /// <summary>Name of the zipped native runtimes embedded in the VSIX, produced by `.CreateZipArchive.ps1` at build time.</summary>
        public const string RuntimeZipFileName = "WhisperNet.Runtime.zip";

        /// <summary>Absolute path of the "runtimes" folder under the extension's working directory where the native Whisper.Net libraries are unpacked.</summary>
        private static readonly string _runtimeFolderPath;

        /// <summary>Display name of this backend, shown in the recorder picker.</summary>
        public const string RecorderName = "Whisper local (using Whisper.Net; processing on Vulkan or CPU)";

        /// <inheritdoc/>
        public string Name => RecorderName;

        /// <summary>Locates the runtimes folder under the working directory and extracts the native libraries into it if they are not there yet.</summary>
        static WhisperNetRecorderFactory()
        {
            // This assembly is deployed next to FreeAIr.dll, so its own folder is the extension's
            // working folder - the same one FreeAIrPackage.WorkingFolder resolves to.
            var workingFolder = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;

            _runtimeFolderPath = Path.Combine(workingFolder, @"runtimes");

            Unpack();
        }

        /// <summary>The control where the user picks the local model file and the decoding prompt.</summary>
        public UserControl CreateConfigurationControl()
        {
            return new WhisperNetUserControl();
        }

        /// <summary>Builds and initializes a <see cref="WhisperNetRecorder"/> from the model path and prompt saved in the recording settings.</summary>
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

        /// <summary>Extracts the embedded runtime zip into the runtimes folder, once, when the folder exists but is still empty.</summary>
        private static void Unpack()
        {
            try
            {
                if (!Directory.Exists(_runtimeFolderPath))
                {
                    return;
                }

                if (Directory.GetFileSystemEntries(_runtimeFolderPath).Length > 1)
                {
                    return;
                }

                var zipFilePath = Path.Combine(
                    _runtimeFolderPath,
                    RuntimeZipFileName
                    );
                using var zip = ZipFile.OpenRead(zipFilePath);
                zip.ExtractToDirectory(_runtimeFolderPath);
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }
    }
}
