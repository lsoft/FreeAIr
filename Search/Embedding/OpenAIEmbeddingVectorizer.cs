using OpenAI;
using OpenAI.Embeddings;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// The shipping vectorizer: an OpenAI compatible embeddings endpoint.
    ///
    /// It lives here rather than in the VSIX so that an integration test can drive the very same
    /// code against a real model — a fake which happens to agree with the ranking proves nothing
    /// about the provider.
    /// </summary>
    public sealed class OpenAIEmbeddingVectorizer : IEmbeddingVectorizer
    {
        private const string NoToken = "no-token";

        /// <summary>
        /// How much of the server's complaint is kept. The interesting part of such an answer is
        /// always at the front, and the whole of it can be a page of json.
        /// </summary>
        private const int MaxReportedBodyLength = 500;

        private readonly EmbeddingClient _embeddingClient;
        private readonly string _endpoint;

        public string ModelName
        {
            get;
        }

        /// <inheritdoc/>
        public string? ReportedModelName
        {
            get;
            private set;
        }

        public OpenAIEmbeddingVectorizer(
            string model,
            string endpoint,
            string? token
            )
        {
            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException($"'{nameof(model)}' cannot be null or whitespace.", nameof(model));
            }

            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException($"'{nameof(endpoint)}' cannot be null or whitespace.", nameof(endpoint));
            }

            ModelName = model;
            _endpoint = endpoint;

            _embeddingClient = new EmbeddingClient(
                model,
                new ApiKeyCredential(
                    //a local embedding server usually wants no key at all, while ApiKeyCredential
                    //refuses an empty string outright. The placeholder goes out as an
                    //Authorization header which such a server does not read.
                    string.IsNullOrWhiteSpace(token)
                        ? NoToken
                        : token!
                    ),
                new OpenAIClientOptions
                {
                    //building the index of a large solution is one long request, and the default
                    //timeout gives up in the middle of it
                    NetworkTimeout = TimeSpan.FromHours(1),
                    Endpoint = new Uri(endpoint),
                }
                );
        }

        public async Task<IReadOnlyList<float[]>> VectorizeAsync(
            IReadOnlyList<string> texts,
            CancellationToken cancellationToken
            )
        {
            if (texts is null)
            {
                throw new ArgumentNullException(nameof(texts));
            }

            if (texts.Count == 0)
            {
                return Array.Empty<float[]>();
            }

            var request = new List<string>(texts.Count);
            request.AddRange(texts);

            ClientResult<OpenAIEmbeddingCollection> embeddings;
            try
            {
                embeddings = await _embeddingClient.GenerateEmbeddingsAsync(
                    request, //TODO split by LLM context size
                    cancellationToken: cancellationToken
                    );
            }
            catch (ClientResultException excp)
            {
                //`Service request failed. Status: 400` is not something a user can act on, while
                //the body underneath it names the model or the parameter the server did not like.
                //It is the only place that knowledge exists, and it is thrown away by default.
                throw new EmbeddingRequestException(
                    BuildFailureMessage(
                        excp.Status,
                        TryReadBody(excp),
                        ModelName,
                        _endpoint,
                        texts.Count
                        ),
                    excp
                    );
            }

            ReportedModelName = TryReadReportedModel(embeddings) ?? ReportedModelName;

            var result = new float[embeddings.Value.Count][];
            for (var i = 0; i < embeddings.Value.Count; i++)
            {
                result[i] = embeddings.Value[i].ToFloats().ToArray();
            }

            return result;
        }

        /// <summary>
        /// What the user is told when the server refuses the request. Everything the answer to
        /// `why` can depend on is in here: which server, which model, how many texts, and whatever
        /// the server itself said — a status code alone leaves the user guessing between a wrong
        /// model name, a wrong endpoint and an agent which cannot do embeddings at all.
        /// </summary>
        internal static string BuildFailureMessage(
            int status,
            string? body,
            string model,
            string endpoint,
            int textCount
            )
        {
            var message =
                $"The embedding server at {endpoint} answered HTTP {status} to a request for {textCount} vector(s) with model '{model}'.";

            if (string.IsNullOrWhiteSpace(body))
            {
                return message + " It gave no reason.";
            }

            var trimmed = body!.Trim();
            if (trimmed.Length > MaxReportedBodyLength)
            {
                trimmed = trimmed.Substring(0, MaxReportedBodyLength) + "...";
            }

            return message + " It said: " + trimmed;
        }

        private static string? TryReadBody(
            ClientResultException exception
            )
        {
            try
            {
                return exception.GetRawResponse()?.Content?.ToString();
            }
            catch (Exception)
            {
                //a response which has already been consumed keeps nothing to read
                return null;
            }
        }

        /// <summary>
        /// Digs the `model` field out of the raw reply. The SDK parses it and then does not expose
        /// it, and it is the only field which tells one local model from another: with the model
        /// name in the request ignored by the server, everything else about two different models
        /// can look identical.
        ///
        /// Failure is not an error — the name is a label, and a server which does not send one
        /// simply leaves it empty.
        /// </summary>
        private static string? TryReadReportedModel(
            ClientResult<OpenAIEmbeddingCollection> result
            )
        {
            try
            {
                var content = result?.GetRawResponse()?.Content;
                if (content is null)
                {
                    return null;
                }

                using var json = JsonDocument.Parse(content.ToMemory());

                if (json.RootElement.ValueKind != JsonValueKind.Object
                    || !json.RootElement.TryGetProperty("model", out var model)
                    || model.ValueKind != JsonValueKind.String)
                {
                    return null;
                }

                var name = model.GetString();
                return string.IsNullOrWhiteSpace(name)
                    ? null
                    : name
                    ;
            }
            catch (Exception)
            {
                //a streamed or already consumed response has no buffered content to read
                return null;
            }
        }
    }

    /// <summary>
    /// The embedding server refused the request. A type of its own so that a caller can tell a
    /// server which said no from a bug on our side, and so that the message — which already carries
    /// everything the user needs — can be shown as it stands.
    /// </summary>
    public sealed class EmbeddingRequestException : Exception
    {
        public EmbeddingRequestException(
            string message,
            Exception innerException
            )
            : base(message, innerException)
        {
        }
    }
}
