using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Record.Fake
{
    public sealed class FakeRecorder : IRecorder
    {
        public static readonly FakeRecorder Instance = new();


        public string Name => "Fake recorder";

        public RecorderStatusEnum Status => RecorderStatusEnum.Idle;

        public event RecorderStatusChangedDelegate RecorderStatusChangedSignal;

        public Task InitAsync()
        {
            //nothing to do
            return Task.CompletedTask;
        }

        public Task<RecordTranscribeResult> RecordAndTranscribeAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(
                RecordTranscribeResult.FromFailure(
                    "Fake recorder cannot record. If you are see this recorder, then something inside FreeAIr went wrong."
                    )
                );
        }

        public async ValueTask DisposeAsync()
        {
            //nothing to do
        }

    }
}
