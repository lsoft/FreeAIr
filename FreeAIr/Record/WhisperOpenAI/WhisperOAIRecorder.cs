using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using OpenAI;
using OpenAI.Audio;
using System.ClientModel;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Record.WhisperOpenAI
{
    public sealed class WhisperOAIRecorder : IRecorder
    {
        private readonly string _modelName;
        private readonly string _token;
        private readonly string _endpoint;
        private readonly string _prompt;

        private RecorderStatusEnum _status = RecorderStatusEnum.Idle;
        private AudioClient? _audioClient;

        public event RecorderStatusChangedDelegate RecorderStatusChangedSignal;

        public string Name => WhisperOAIRecorderFactory.RecorderName;

        public RecorderStatusEnum Status
        {
            get => _status;
            private set
            {
                _status = value;

                RecorderStatusChangedSignal?.Invoke(this, value);
            }
        }

        public WhisperOAIRecorder(
            string modelName,
            string token,
            string endpoint,
            string prompt
            )
        {
            if (modelName is null)
            {
                throw new ArgumentNullException(nameof(modelName));
            }

            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (endpoint is null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (prompt is null)
            {
                throw new ArgumentNullException(nameof(prompt));
            }

            _modelName = modelName;
            _token = token;
            _endpoint = endpoint;
            _prompt = prompt;
        }

        public Task InitAsync()
        {
            _audioClient = new OpenAI.Audio.AudioClient(
                model: _modelName,
                new ApiKeyCredential(
                    DirectOrEnvStringHelper.GetValue(_token)
                    ),
                new OpenAIClientOptions
                {
                    NetworkTimeout = TimeSpan.FromHours(1),
                    Endpoint = new Uri(_endpoint)
                }
                );


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

                using var recognizeFile = await MicrophoneRecorder.RecordAsync(
                    recordingCancellationToken
                    );

                Status = RecorderStatusEnum.Transcribing;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

                var options = new AudioTranscriptionOptions()
                {
                    Prompt = _prompt
                    //ResponseFormat = AudioTranscriptionFormat.Verbose, // Get more details like timestamps
                    //Includes = AudioTranscriptionIncludes.Logprobs
                };

                var r = await _audioClient.TranscribeAudioAsync(
                    recognizeFile.FilePath,
                    options
                    );
                var text = r.Value.Text;

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.


                return RecordTranscribeResult.FromSuccess(text);
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
            //nothing to do
        }

    }

}
