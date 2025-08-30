using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;

namespace FreeAIr.Record.WinRT
{
    public sealed class WinRTRecorder : IRecorder
    {
        private RecorderStatusEnum _status = RecorderStatusEnum.Idle;
        
        private SpeechRecognizer? _recognizer;

        public string Name => WinRTRecorderFactory.RecorderName;

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


        public async Task InitAsync()
        {
            var dictation = new SpeechRecognitionTopicConstraint(
                SpeechRecognitionScenario.Dictation,
                "FreeAIr dictation"
                );
            _recognizer = new SpeechRecognizer(
                );
            _recognizer.Constraints.Add(dictation);

            //Ensures that the speech recognizer's constraints are compiled before starting recognition.
            //This is necessary to prepare the recognizer for accurate speech recognition.
            await RunPayloadWithAccessProtectionAsync(
                () => _recognizer.CompileConstraintsAsync().AsTask()
                );
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

                var recognizedText = await new WinRTRecordStopCallAwaiter(
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

        private static async Task RunPayloadWithAccessProtectionAsync(
            Func<Task> payload
            )
        {
            try
            {
                await payload();
            }
            catch (Exception ex) when (ex.Message.Contains("privacy"))
            {
                ex.ActivityLogException();

                var confirmed = await VS.MessageBox.ShowConfirmAsync(
                    "Enable Windows speech recognition",
                    "Enable Windows speech recognition to use this feature. Do you want to open the settings?"
                    );
                if (confirmed)
                {
                    // Open the settings page for speech recognition
                    _ = await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-speech"));
                }

                throw;
            }

        }


        private sealed class WinRTRecordStopCallAwaiter : CallAwaiter<string>
        {
            private readonly WinRTRecorder _recorder;
            private readonly CancellationToken _recordingCancellationToken;
            
            private string? _recognizedText;

            public WinRTRecordStopCallAwaiter(
                WinRTRecorder recorder,
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
                _recorder._recognizer.ContinuousRecognitionSession.ResultGenerated += ResultGenerated;

                await _recorder._recognizer.ContinuousRecognitionSession.StartAsync().AsTask();

                while (!_recordingCancellationToken.IsCancellationRequested)
                {
                    await Task.Yield();
                }

                _recorder.Status = RecorderStatusEnum.Transcribing;

                await _recorder._recognizer.ContinuousRecognitionSession.StopAsync().AsTask();
            }

            protected override Task<string> GetResultAsync()
            {
                return Task.FromResult(
                    _recognizedText
                    );
            }

            protected override Task CleanupAsync()
            {
                _recorder._recognizer.ContinuousRecognitionSession.ResultGenerated -= ResultGenerated;
                
                return Task.CompletedTask;
            }

            private void ResultGenerated(
                SpeechContinuousRecognitionSession sender,
                SpeechContinuousRecognitionResultGeneratedEventArgs args
                )
            {
                if (args.Result.Status == SpeechRecognitionResultStatus.Success)
                {
                    _recognizedText = args.Result.Text;
                }

                Fire();
            }
        }
    }
}
