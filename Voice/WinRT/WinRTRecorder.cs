using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.SpeechRecognition;
using FreeAIr.BLogic;

namespace FreeAIr.Record.WinRT
{
    /// <summary>
    /// Speech-to-text using the Windows Runtime <see cref="SpeechRecognizer"/> (`Windows.Media.SpeechRecognition`)
    /// in dictation mode. Distinct from <see cref="Speech.SpeechRecorder"/>, which uses the older
    /// `System.Speech` API; this one prompts the user to enable Windows speech recognition privacy
    /// settings if they are turned off.
    /// </summary>
    public sealed class WinRTRecorder : IRecorder
    {
        /// <summary>Backing field for <see cref="Status"/>.</summary>
        private RecorderStatusEnum _status = RecorderStatusEnum.Idle;

        /// <summary>The WinRT speech recognizer driving continuous dictation, built and compiled in <see cref="InitAsync"/>.</summary>
        private SpeechRecognizer? _recognizer;

        /// <inheritdoc/>
        public string Name => WinRTRecorderFactory.RecorderName;

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


        /// <summary>Builds the recognizer with a dictation constraint and compiles it, prompting for the speech privacy setting if it is off.</summary>
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

        /// <summary>Records via the WinRT continuous recognition session until cancelled, returning the last recognized phrase.</summary>
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

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            _recognizer?.Dispose();
        }

        /// <summary>
        /// Runs a WinRT speech call, and when it fails because the Windows speech privacy setting is
        /// off, offers to open the settings page for it before rethrowing.
        /// </summary>
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


        /// <summary>
        /// Bridges the WinRT continuous recognition session's event-driven API to an awaitable call:
        /// starts the session, waits for the cancellation token, then stops it and returns the last
        /// recognized phrase.
        /// </summary>
        private sealed class WinRTRecordStopCallAwaiter : CallAwaiter<string>
        {
            /// <summary>The recorder whose recognition session this awaiter starts, drives, and stops.</summary>
            private readonly WinRTRecorder _recorder;
            /// <summary>Signals when the continuous recognition session should stop.</summary>
            private readonly CancellationToken _recordingCancellationToken;

            /// <summary>The most recent successfully recognized phrase, updated as recognition results arrive.</summary>
            private string? _recognizedText;

            /// <summary>Wraps the recorder whose recognition session is driven and the token that signals when to stop.</summary>
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

            /// <summary>Starts the continuous recognition session, waits for the cancellation token, then stops it.</summary>
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

            /// <summary>The last successfully recognized phrase before the session was stopped.</summary>
            protected override Task<string> GetResultAsync()
            {
                return Task.FromResult(
                    _recognizedText
                    );
            }

            /// <summary>Detaches the result-generated event handler.</summary>
            protected override Task CleanupAsync()
            {
                _recorder._recognizer.ContinuousRecognitionSession.ResultGenerated -= ResultGenerated;

                return Task.CompletedTask;
            }

            /// <summary>Captures each successful recognition result as it comes in, during the continuous session.</summary>
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
