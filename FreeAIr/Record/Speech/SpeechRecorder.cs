using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using System.Speech.Recognition;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Record.Speech
{
    public sealed class SpeechRecorder : IRecorder
    {
        private RecorderStatusEnum _status = RecorderStatusEnum.Idle;

        private SpeechRecognitionEngine? _recognizer;

        public string Name => SpeechRecorderFactory.RecorderName;

        public event RecorderStatusChangedDelegate RecorderStatusChangedSignal;


        public RecorderStatusEnum Status
        {
            get => _status;
            private set
            {
                _status = value;

                RecorderStatusChangedSignal?.Invoke(this, value);
            }
        }


        public Task InitAsync()
        {
            _recognizer = new SpeechRecognitionEngine(
                );
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.LoadGrammar(new DictationGrammar());

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

                var recognizedText = await new SpeechRecordStopCallAwaiter(
                    this,
                    recordingCancellationToken
                    ).WaitForCallAsync();

                return RecordTranscribeResult.FromSuccess(recognizedText ?? string.Empty);
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
            _recognizer?.Dispose();
        }


        private sealed class SpeechRecordStopCallAwaiter : CallAwaiter<string>
        {
            private readonly SpeechRecorder _recorder;
            private readonly CancellationToken _recordingCancellationToken;

            private string? _recognizedText;

            public SpeechRecordStopCallAwaiter(
                SpeechRecorder recorder,
                CancellationToken recordingCancellationToken
                ) : base(TimeSpan.FromSeconds(1))
            {
                if (recorder is null)
                {
                    throw new ArgumentNullException(nameof(recorder));
                }

                _recorder = recorder;
                _recordingCancellationToken = recordingCancellationToken;
            }

            protected override async Task PrepareAsync()
            {
                _recorder._recognizer.SpeechRecognized += SpeechRecognized;

                _recorder._recognizer.RecognizeAsync(RecognizeMode.Multiple);

                while (!_recordingCancellationToken.IsCancellationRequested)
                {
                    await Task.Yield();
                }

                _recorder.Status = RecorderStatusEnum.Transcribing;

                _recorder._recognizer.RecognizeAsyncStop();
            }

            protected override Task<string> GetResultAsync()
            {
                return Task.FromResult(
                    _recognizedText
                    );
            }

            protected override Task CleanupAsync()
            {
                _recorder._recognizer.SpeechRecognized -= SpeechRecognized;

                return Task.CompletedTask;
            }

            private void SpeechRecognized(
                object sender,
                SpeechRecognizedEventArgs args
                )
            {
                _recognizedText = args.Result.Text;

                Fire();
            }
        }
    }

}
