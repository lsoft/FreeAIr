using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Record
{
    /// <summary>
    /// A speech-to-text backend: captures audio from the microphone and turns it into text.
    /// FreeAIr ships several implementations (Microsoft Speech API, WinRT, local Whisper.Net, Whisper
    /// over an OpenAI-compatible endpoint, and the no-op <see cref="Fake.FakeRecorder"/>); the active
    /// one is selected by <see cref="ChosenRecorder"/>.
    /// </summary>
    public interface IRecorder : IAsyncDisposable
    {
        /// <summary>Display name shown in the recorder picker menu.</summary>
        string Name
        {
            get;
        }

        /// <summary>Whether the recorder is idle, currently recording, or transcribing what it captured.</summary>
        RecorderStatusEnum Status
        {
            get;
        }

        /// <summary>Fires whenever <see cref="Status"/> changes, so the UI can update its icon.</summary>
        public event RecorderStatusChangedDelegate RecorderStatusChangedSignal;

        /// <summary>Prepares the backend for use — loading a model, opening a device — before the first recording.</summary>
        Task InitAsync();

        /// <summary>Records until the token is cancelled, then transcribes what was captured into text.</summary>
        Task<RecordTranscribeResult> RecordAndTranscribeAsync(
            CancellationToken recordingCancellationToken
            );
    }

    /// <summary>Signature of <see cref="IRecorder.RecorderStatusChangedSignal"/>.</summary>
    public delegate void RecorderStatusChangedDelegate(IRecorder sender, RecorderStatusEnum newStatus);

    /// <summary>The states an <see cref="IRecorder"/> can report through <see cref="IRecorder.Status"/>.</summary>
    public enum RecorderStatusEnum
    {
        /// <summary>Not recording and not transcribing.</summary>
        Idle,

        /// <summary>Capturing audio from the microphone.</summary>
        Recording,

        /// <summary>Converting captured audio into text.</summary>
        Transcribing
    }
}
