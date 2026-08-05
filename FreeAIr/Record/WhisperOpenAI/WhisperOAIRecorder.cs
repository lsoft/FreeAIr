using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using OpenAI;
using OpenAI.Audio;
using System.ClientModel;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Record.WhisperOpenAI
{
    /// <summary>
    /// Speech-to-text via an OpenAI-compatible audio transcription endpoint (OpenAI itself, or any
    /// server implementing the same `/audio/transcriptions` API, such as a local Whisper server).
    /// Used when dictation should run on a cloud model or a self-hosted server rather than in-process.
    /// </summary>
    public sealed class WhisperOAIRecorder : IRecorder
    {
        /// <summary>Name of the model to request transcriptions from at the configured endpoint.</summary>
        private readonly string _modelName;

        /// <summary>API token (or environment variable reference) used to authenticate with the endpoint.</summary>
        private readonly string _token;

        /// <summary>Base URL of the OpenAI-compatible audio transcription endpoint.</summary>
        private readonly string _endpoint;

        /// <summary>Decoding prompt passed to the transcription API to bias its output (e.g. domain vocabulary).</summary>
        private readonly string _prompt;

        /// <summary>Backing field for <see cref="Status"/>.</summary>
        private RecorderStatusEnum _status = RecorderStatusEnum.Idle;

        /// <summary>The configured OpenAI audio client used to send transcription requests, created by <see cref="InitAsync"/>.</summary>
        private AudioClient? _audioClient;

        /// <inheritdoc/>
        public event RecorderStatusChangedDelegate RecorderStatusChangedSignal;

        /// <inheritdoc/>
        public string Name => WhisperOAIRecorderFactory.RecorderName;

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

        /// <summary>Wraps the model name, token, endpoint and decoding prompt taken from the recording settings.</summary>
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

        /// <summary>Builds the audio client for the configured endpoint, with a generous one-hour timeout for slow local servers.</summary>
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

        /// <summary>Records to a temporary wav file via <see cref="MicrophoneRecorder"/>, then uploads it to the configured endpoint for transcription.</summary>
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

        /// <summary>No-op; the audio client holds no unmanaged resources to release.</summary>
        public async ValueTask DisposeAsync()
        {
            //nothing to do
        }

    }

}
