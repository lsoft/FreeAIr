using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Record.Fake
{
    /// <summary>
    /// A do-nothing <see cref="IRecorder"/> that always reports failure instead of recording. It is
    /// what <see cref="ChosenRecorder"/> falls back to when the configured speech-to-text backend
    /// could not be created, so voice dictation degrades to a clear error instead of a crash.
    /// </summary>
    public sealed class FakeRecorder : IRecorder
    {
        /// <summary>The single shared instance; there is never a reason to have more than one.</summary>
        public static readonly FakeRecorder Instance = new();


        /// <inheritdoc/>
        public string Name => "Fake recorder";

        /// <inheritdoc/>
        public RecorderStatusEnum Status => RecorderStatusEnum.Idle;

        /// <inheritdoc/>
        public event RecorderStatusChangedDelegate RecorderStatusChangedSignal;

        /// <summary>No-op; this recorder needs no setup.</summary>
        public Task InitAsync()
        {
            //nothing to do
            return Task.CompletedTask;
        }

        /// <summary>Always returns a failure result explaining that no real recorder could be set up.</summary>
        public Task<RecordTranscribeResult> RecordAndTranscribeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(
                RecordTranscribeResult.FromFailure(
                    "Fake recorder cannot record. If you are see this message, then something inside FreeAIr went wrong."
                    )
                );
        }

        /// <summary>No-op; this recorder holds nothing to release.</summary>
        public async ValueTask DisposeAsync()
        {
            //nothing to do
        }

    }
}
