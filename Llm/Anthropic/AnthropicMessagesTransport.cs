using FreeAIr.Llm.Wire;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Llm.Anthropic
{
    /// <summary>
    /// Speaks the Anthropic messages API (`POST /v1/messages`), which Claude models served by
    /// Anthropic itself understand and the OpenAI protocol is not a dialect of.
    ///
    /// Hand written rather than built on an SDK: the extension runs on .NET Framework 4.8 and no
    /// Anthropic client targets it, so the wire is `HttpClient` plus a server-sent event reader and
    /// <see cref="AnthropicRequestWriter"/>.
    /// </summary>
    public sealed class AnthropicMessagesTransport : ILlmTransport
    {
        /// <summary>
        /// The version of the API this transport writes. Anthropic requires the header on every
        /// request and pins the request and response shapes to it, so it belongs here rather than
        /// in the settings.
        /// </summary>
        public const string AnthropicVersion = "2023-06-01";

        /// <summary>
        /// What `max_tokens` becomes when the settings name no limit. Unlike OpenAI, this API makes
        /// the field mandatory and rejects a request without it, so there has to be a number.
        /// </summary>
        public const int DefaultMaxOutputTokens = 8192;

        /// <summary>
        /// Deliberately huge, for the same reason the OpenAI transport's is: a local model on a slow
        /// machine can think for a long time.
        /// </summary>
        public static readonly TimeSpan NetworkTimeout = TimeSpan.FromHours(1);

        private readonly Uri _endpoint;
        private readonly string _token;
        private readonly HttpMessageHandler? _handler;

        /// <summary>
        /// Binds a transport to one endpoint and token. <paramref name="handler"/> is the seam the
        /// tests answer with a canned stream through; leave it null in production.
        /// </summary>
        public AnthropicMessagesTransport(
            Uri endpoint,
            string? token,
            HttpMessageHandler? handler = null
            )
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _token = token ?? string.Empty;
            _handler = handler;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LlmStreamEvent> StreamAsync(
            LlmRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken
            )
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            using var client = CreateHttpClient();
            using var httpRequest = CreateHttpRequest(request);

            var response = await client.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
                );

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    throw await ReadFailureAsync(response);
                }

#if NET
                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
#else
                using var stream = await response.Content.ReadAsStreamAsync();
#endif

                var finishReason = LlmFinishReason.Unknown;

                await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
                {
                    //the event name is taken from the payload rather than from the `event:` line:
                    //a proxy in between is free to drop the line, and the payload always says what
                    //it is
                    foreach (var streamEvent in ReadEvent(item.Data, ref finishReason))
                    {
                        yield return streamEvent;

                        if (streamEvent is LlmProtocolFaultEvent)
                        {
                            //an error arrives inside an otherwise successful response - there is no
                            //status code to notice and nothing further to read
                            yield break;
                        }
                    }
                }

                yield return new LlmFinishedEvent(finishReason);
            }
        }

        /// <summary>
        /// Translates one server-sent event into what the chat understands. Returns nothing for the
        /// events which carry no news - `ping`, the block boundaries, the thinking of a model
        /// reasoning aloud.
        /// </summary>
        private static IReadOnlyList<LlmStreamEvent> ReadEvent(
            string data,
            ref LlmFinishReason finishReason
            )
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(data);
            }
            catch (JsonException)
            {
                //a fragment which is not json at all is not something this protocol ever sends;
                //skipping it is better than failing a turn which may still be readable
                return Array.Empty<LlmStreamEvent>();
            }

            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object
                    || !root.TryGetProperty("type", out var typeElement)
                    || typeElement.ValueKind != JsonValueKind.String)
                {
                    return Array.Empty<LlmStreamEvent>();
                }

                switch (typeElement.GetString())
                {
                    case "content_block_start":
                        return ReadContentBlockStart(root);

                    case "content_block_delta":
                        return ReadContentBlockDelta(root);

                    case "message_delta":
                        if (root.TryGetProperty("delta", out var messageDelta)
                            && messageDelta.ValueKind == JsonValueKind.Object
                            && messageDelta.TryGetProperty("stop_reason", out var stopReason)
                            && stopReason.ValueKind == JsonValueKind.String)
                        {
                            finishReason = MapFinishReason(stopReason.GetString());
                        }
                        return Array.Empty<LlmStreamEvent>();

                    case "error":
                        return new LlmStreamEvent[]
                        {
                            new LlmProtocolFaultEvent(ReadErrorMessage(root)),
                        };

                    default:
                        return Array.Empty<LlmStreamEvent>();
                }
            }
        }

        /// <summary>
        /// A new content block. Only a `tool_use` one is news: it is where the id and the name of a
        /// call are announced, before any of its arguments have arrived.
        /// </summary>
        private static IReadOnlyList<LlmStreamEvent> ReadContentBlockStart(
            JsonElement root
            )
        {
            if (!root.TryGetProperty("content_block", out var block)
                || block.ValueKind != JsonValueKind.Object
                || !block.TryGetProperty("type", out var blockType)
                || !blockType.ValueEquals("tool_use"))
            {
                return Array.Empty<LlmStreamEvent>();
            }

            return new LlmStreamEvent[]
            {
                new LlmToolCallOpenedEvent(
                    ReadIndex(root),
                    ReadString(block, "id"),
                    ReadString(block, "name")
                    ),
            };
        }

        /// <summary>
        /// A piece of a content block. Answer text and the arguments of a call arrive this way; a
        /// thinking delta does not become answer text, because that reasoning is not what the chat
        /// window shows and every caller which parses an answer would have to strip it again.
        /// </summary>
        private static IReadOnlyList<LlmStreamEvent> ReadContentBlockDelta(
            JsonElement root
            )
        {
            if (!root.TryGetProperty("delta", out var delta)
                || delta.ValueKind != JsonValueKind.Object
                || !delta.TryGetProperty("type", out var deltaType))
            {
                return Array.Empty<LlmStreamEvent>();
            }

            if (deltaType.ValueEquals("text_delta"))
            {
                var text = ReadString(delta, "text");

                return string.IsNullOrEmpty(text)
                    ? Array.Empty<LlmStreamEvent>()
                    : new LlmStreamEvent[] { new LlmTextDeltaEvent(text) };
            }

            if (deltaType.ValueEquals("input_json_delta"))
            {
                var partial = ReadString(delta, "partial_json");

                return string.IsNullOrEmpty(partial)
                    ? Array.Empty<LlmStreamEvent>()
                    : new LlmStreamEvent[]
                    {
                        new LlmToolCallArgumentsDeltaEvent(ReadIndex(root), partial),
                    };
            }

            return Array.Empty<LlmStreamEvent>();
        }

        /// <summary>The complaint inside an `error` event, or a generic sentence when it carries none.</summary>
        private static string ReadErrorMessage(
            JsonElement root
            )
        {
            if (root.TryGetProperty("error", out var error)
                && error.ValueKind == JsonValueKind.Object)
            {
                var message = ReadString(error, "message");
                if (!string.IsNullOrEmpty(message))
                {
                    return message;
                }
            }

            return "Server returns error.";
        }

        /// <summary>Builds the request: the endpoint, the two mandatory headers and the JSON body.</summary>
        private HttpRequestMessage CreateHttpRequest(
            LlmRequest request
            )
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, BuildMessagesUri())
            {
                Content = new StringContent(
                    AnthropicRequestWriter.Write(request, DefaultMaxOutputTokens),
                    Encoding.UTF8,
                    "application/json"
                    ),
            };

            httpRequest.Headers.Add("x-api-key", _token);
            httpRequest.Headers.Add("anthropic-version", AnthropicVersion);
            httpRequest.Headers.Add("accept", "text/event-stream");

            return httpRequest;
        }

        /// <summary>
        /// The messages endpoint under the configured base.
        ///
        /// A base which already ends in `/v1` is not given a second one: the agent editor's endpoint
        /// field is filled in with OpenAI style bases ending that way, and a user switching an
        /// existing agent over to this protocol will leave it as it was.
        /// </summary>
        private Uri BuildMessagesUri()
        {
            var text = _endpoint.ToString().TrimEnd('/');

            if (text.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return new Uri(text + "/messages");
            }

            return new Uri(text + "/v1/messages");
        }

        /// <summary>
        /// A client for one request. The injected handler is never disposed with it: it belongs to
        /// the test which is about to read what was sent through it.
        /// </summary>
        private HttpClient CreateHttpClient()
        {
            var client = _handler is null
                ? new HttpClient()
                : new HttpClient(_handler, disposeHandler: false);

            client.Timeout = NetworkTimeout;

            return client;
        }

        /// <summary>Turns a non-success response into the neutral failure, keeping the body it came with.</summary>
        private static async Task<LlmTransportException> ReadFailureAsync(
            HttpResponseMessage response
            )
        {
            string? body = null;
            try
            {
                body = await response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                //a response whose body cannot be read still has a status worth reporting
            }

            return new LlmTransportException(
                $"Service request failed. Status: {(int)response.StatusCode}",
                ServerErrorMessageReader.Read(body),
                (int)response.StatusCode
                );
        }

        /// <summary>Maps this protocol's stop reasons onto the neutral ones.</summary>
        private static LlmFinishReason MapFinishReason(
            string? stopReason
            )
        {
            switch (stopReason)
            {
                case "tool_use":
                    return LlmFinishReason.ToolCalls;
                case "end_turn":
                case "stop_sequence":
                    return LlmFinishReason.Stop;
                case "max_tokens":
                    return LlmFinishReason.Length;
                case "refusal":
                    return LlmFinishReason.ContentFilter;
                default:
                    return LlmFinishReason.Unknown;
            }
        }

        /// <summary>The content block index an event names, or zero when it names none.</summary>
        private static int ReadIndex(
            JsonElement root
            )
        {
            return root.TryGetProperty("index", out var index) && index.TryGetInt32(out var value)
                ? value
                : 0;
        }

        /// <summary>A string member, or an empty string when it is absent or of another kind.</summary>
        private static string ReadString(
            JsonElement element,
            string name
            )
        {
            return element.TryGetProperty(name, out var member) && member.ValueKind == JsonValueKind.String
                ? member.GetString() ?? string.Empty
                : string.Empty;
        }
    }
}
