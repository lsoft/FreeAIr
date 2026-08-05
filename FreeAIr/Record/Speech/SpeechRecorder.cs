using Microsoft.VisualStudio.Threading;
using System.Speech.Recognition;
using System.Threading;
using System.Threading.Tasks;
using FreeAIr.BLogic;

namespace FreeAIr.Record.Speech
{
    /// <summary>
    /// Speech-to-text backed by the built-in Windows Speech Recognition (`System.Speech`), used for
    /// dictating prompts without any external model or network endpoint.
    /// </summary>
    public sealed class SpeechRecorder : IRecorder
    {
        /// <summary>Backing field for <see cref="Status"/>.</summary>
        private RecorderStatusEnum _status = RecorderStatusEnum.Idle;

        /// <summary>The Windows Speech Recognition engine driving continuous dictation, created in <see cref="InitAsync"/>.</summary>
        private SpeechRecognitionEngine? _recognizer;

        /// <inheritdoc/>
        public string Name => SpeechRecorderFactory.RecorderName;

        /// <inheritdoc/>
        public event RecorderStatusChangedDelegate RecorderStatusChangedSignal;


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


        /// <summary>Creates the recognition engine on the default microphone, with dictation grammar loaded.</summary>
        public Task InitAsync()
        {
            _recognizer = new SpeechRecognitionEngine(
                );
            _recognizer.SetInputToDefaultAudioDevice();
            _recognizer.LoadGrammar(new DictationGrammar());

            return Task.CompletedTask;
        }

        /// <summary>Records until cancelled and returns whatever the recognizer produced from continuous dictation.</summary>
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

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            _recognizer?.Dispose();
        }


        /// <summary>
        /// Bridges the recognizer's event-driven API to an awaitable call: starts continuous
        /// recognition, waits for the cancellation token, then stops and returns the last recognized
        /// phrase.
        /// </summary>
        private sealed class SpeechRecordStopCallAwaiter : CallAwaiter<string>
        {
            /// <summary>The recorder whose recognizer this awaiter starts, drives, and stops.</summary>
            private readonly SpeechRecorder _recorder;
            /// <summary>Signals when continuous recognition should stop.</summary>
            private readonly CancellationToken _recordingCancellationToken;

            /// <summary>The most recent phrase reported by the recognizer, updated as recognition events arrive.</summary>
            private string? _recognizedText;

            /// <summary>Wraps the recorder whose recognizer is driven and the token that signals when to stop.</summary>
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

            /// <summary>Starts continuous recognition, waits for the cancellation token, then stops it.</summary>
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

            /// <summary>The last phrase the recognizer reported before recognition was stopped.</summary>
            protected override Task<string> GetResultAsync()
            {
                return Task.FromResult(
                    _recognizedText
                    );
            }

            /// <summary>Detaches the recognized-speech event handler.</summary>
            protected override Task CleanupAsync()
            {
                _recorder._recognizer.SpeechRecognized -= SpeechRecognized;

                return Task.CompletedTask;
            }

            /// <summary>Captures the recognizer's latest result as it comes in, during continuous dictation.</summary>
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
