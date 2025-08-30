using NAudio.Wave;
using System.Threading;
using System.Threading.Tasks;
using FreeAIr.BLogic;

namespace FreeAIr.Record
{
    public static class MicrophoneRecorder
    {
        private static readonly WaveFormat _waveFormat = new WaveFormat(16000, 16, 1); // 16kHz, 16-bit, Mono

        public static async Task<TempFile> RecordAsync(
            CancellationToken cancellationToken
            )
        {
            var recordFile = TempFile.CreateWithExtension("wav");

            using var tempRecordStream = recordFile.OpenWrite();
            using (var writer = new NAudio.Wave.WaveFileWriter(tempRecordStream, _waveFormat))
            {
                using (var waveIn = new WaveInEvent
                {
                    WaveFormat = _waveFormat
                })
                {
                    waveIn.DataAvailable += (sender, args) =>
                    {
                        if (args.BytesRecorded > 0)
                        {
                            writer.Write(args.Buffer, 0, args.BytesRecorded);
                        }
                    };

                    waveIn.StartRecording();

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.Yield();
                    }

                    waveIn.StopRecording();
                }

                await writer.FlushAsync();
            }

            return recordFile;
        }

    }
}
