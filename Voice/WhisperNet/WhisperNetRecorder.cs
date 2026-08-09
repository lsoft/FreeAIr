using Microsoft.VisualStudio.Threading;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.LibraryLoader;

namespace FreeAIr.Record.WhisperNet
{
    /// <summary>
    /// Speech-to-text using a local Whisper.Net model file, processed on the GPU via Vulkan when
    /// available and falling back to the CPU. Used for offline dictation with no cloud endpoint and
    /// no per-request cost.
    /// </summary>
    public sealed class WhisperNetRecorder : IRecorder
    {
        /// <summary>Path to the local Whisper `.bin` model file to load.</summary>
        private readonly string _modelFilePath;
        /// <summary>Decoding prompt passed to Whisper as a vocabulary/style hint.</summary>
        private readonly string _prompt;

        /// <summary>Loaded native Whisper model, created once in <see cref="InitAsync"/> and disposed in <see cref="DisposeAsync"/>.</summary>
        private WhisperFactory? _whisperFactory;
        /// <summary>The built Whisper processor used to transcribe recorded audio.</summary>
        private WhisperProcessor _processor;
        /// <summary>Backing field for <see cref="Status"/>.</summary>
        private RecorderStatusEnum _status = RecorderStatusEnum.Idle;

        /// <inheritdoc/>
        public event RecorderStatusChangedDelegate RecorderStatusChangedSignal;

        /// <inheritdoc/>
        public string Name => WhisperNetRecorderFactory.RecorderName;

        /// <inheritdoc/>
        public RecorderStatusEnum Status
        {
            get => _status;
            private set
            {
                _status = value;

                RecorderStatusChangedSignal?.Invoke(this, value);
            }
        }

        /// <summary>Wraps the local `.bin` model file path and the Whisper decoding prompt (a hint text steering vocabulary/style), both taken from the recording settings.</summary>
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

        /// <summary>Loads the local model and builds the Whisper processor, preferring the GPU and auto-detecting the spoken language.</summary>
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

        /// <summary>Records to a temporary wav file via <see cref="MicrophoneRecorder"/>, then runs it through the local Whisper model, preferring Vulkan and falling back to CPU.</summary>
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

        /// <summary>Releases the Whisper processor and the native model factory.</summary>
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
