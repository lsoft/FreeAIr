using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Record
{
    public interface IRecorder : IAsyncDisposable
    {
        string Name
        {
            get;
        }

        RecorderStatusEnum Status
        {
            get;
        }

        public event RecorderStatusChangedDelegate RecorderStatusChangedSignal;

        Task InitAsync();

        Task<RecordTranscribeResult> RecordAndTranscribeAsync(
            CancellationToken recordingCancellationToken
            );
    }

    public delegate void RecorderStatusChangedDelegate(IRecorder sender, RecorderStatusEnum newStatus);

    public enum RecorderStatusEnum
    {
        Idle,
        Recording,
        Transcribing
    }
}
