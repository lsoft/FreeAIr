using Microsoft.VisualStudio.Threading;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.LibraryLoader;

namespace FreeAIr.Record.WhisperNet
{
    public sealed class WhisperNetRecorder : IRecorder
    {
        private readonly string _modelFilePath;
        private readonly string _prompt;

        private WhisperFactory? _whisperFactory;
        private WhisperProcessor _processor;
        private RecorderStatusEnum _status = RecorderStatusEnum.Idle;

        public event RecorderStatusChangedDelegate RecorderStatusChangedSignal;

        public string Name => WhisperNetRecorderFactory.RecorderName;

        public RecorderStatusEnum Status
        {
            get => _status;
            private set
            {
                _status = value;

                RecorderStatusChangedSignal?.Invoke(this, value);
            }
        }

        public WhisperNetRecorder(
            string modelFilePath,
            string prompt
            )
        {
            if (modelFilePath is null)
            {
                throw new ArgumentNullException(nameof(modelFilePath));
            }

            if (prompt is null)
            {
                throw new ArgumentNullException(nameof(prompt));
            }

            _modelFilePath = modelFilePath;
            _prompt = prompt;
        }

        public Task InitAsync()
        {
            _whisperFactory = WhisperFactory.FromPath(
                _modelFilePath,
                new WhisperFactoryOptions
                {
                    //UseFlashAttention = true,
                    UseGpu = true
                }
                );

            _processor = _whisperFactory.CreateBuilder()
                .WithPrompt(_prompt)
                .WithLanguage("auto")
                .Build()
                ;

            return Task.CompletedTask;
        }

        public async Task<RecordTranscribeResult> RecordAndTranscribeAsync(
            CancellationToken recordingCancellationToken
            )
        {
            try
            {
                Status = RecorderStatusEnum.Recording;

                //switch to background thread (the thread will be blocked)
                await TaskScheduler.Default;

                RuntimeOptions.RuntimeLibraryOrder =
                [
                    RuntimeLibrary.Vulkan,
                    RuntimeLibrary.Cpu
                ];

                using var recognizeFile = await MicrophoneRecorder.RecordAsync(
                    recordingCancellationToken
                    );

                Status = RecorderStatusEnum.Transcribing;

                var textBuilder = new StringBuilder();

                using var recordedFile = recognizeFile.OpenRead();

                await foreach (var result in _processor.ProcessAsync(
                        recordedFile,
                        CancellationToken.None
                        )
                    )
                {
                    //Console.WriteLine($"{result.Start}->{result.End}: {result.Text}");
                    textBuilder.Append(result.Text);
                }

                return RecordTranscribeResult.FromSuccess(textBuilder.ToString());
            }
            catch (Exception excp)
            {
                return RecordTranscribeResult.FromFailure(excp);
            }
            finally
            {
                Status = RecorderStatusEnum.Idle;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_processor is not null)
            {
                await _processor.DisposeAsync();
            }
            _processor = null;

            _whisperFactory?.Dispose();
            _whisperFactory = null;
        }

    }
}
