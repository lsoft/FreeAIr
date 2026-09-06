using FreeAIr.Llm.Wire;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FreeAIr.Llm.OpenAi
{
    /// <summary>
    /// Speaks the OpenAI chat completions protocol - the one every local server (LM Studio,
    /// KoboldCpp, Ollama, text-generation-webui) and most gateways (OpenRouter, Yandex) implement,
    /// which is why it is the default.
    ///
    /// The wire work is done by the OpenAI SDK; what this class adds is the mapping to and from the
    /// neutral model and the failure handling the chat depends on.
    /// </summary>
    public sealed class OpenAiChatTransport : ILlmTransport
    {
        /// <summary>
        /// Deliberately huge: a local LLM on a slow machine can think for a long time, and a
        /// timeout in the middle of a streaming answer is indistinguishable from a broken model.
        /// </summary>
        public static readonly TimeSpan NetworkTimeout = TimeSpan.FromHours(1);

        private readonly Uri _endpoint;
        private readonly string _token;
        private readonly PipelineTransport? _pipelineTransport;

        /// <summary>
        /// Binds a transport to one endpoint and token. <paramref name="pipelineTransport"/> is the
        /// seam the tests use to answer with a canned stream instead of reaching a server; leave it
        /// null in production.
        /// </summary>
        public OpenAiChatTransport(
            Uri endpoint,
            string? token,
            PipelineTransport? pipelineTransport = null
            )
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _token = token ?? string.Empty;
            _pipelineTransport = pipelineTransport;
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

            var client = CreateChatClient(request.Model);
            var messages = OpenAiRequestBuilder.BuildMessages(request);
            var options = OpenAiRequestBuilder.BuildOptions(request);

            var updates = client.CompleteChatStreamingAsync(
                messages: messages,
                options: options,
                cancellationToken: cancellationToken
                );

            var finishReason = LlmFinishReason.Unknown;

            var enumerator = updates.GetAsyncEnumerator(cancellationToken);
            try
            {
                while (true)
                {
                    StreamingChatCompletionUpdate update;
                    try
                    {
                        if (!await enumerator.MoveNextAsync())
                        {
                            break;
                        }

                        update = enumerator.Current;
                    }
                    catch (ClientResultException excp)
                    {
                        //the SDK reports every non-success status this way, and its own message is
                        //only the status line - the body is where the provider says what is wrong
                        throw ToTransportException(excp);
                    }

                    //a chunk without a completion id means the endpoint replied with something
                    //which is not a completion at all (an error page, a quota message, etc.);
                    //there is nothing to read further
                    if (update.CompletionId is null)
                    {
                        yield return new LlmProtocolFaultEvent("Server returns error.");
                        yield break;
                    }

                    if (update.FinishReason.HasValue && finishReason == LlmFinishReason.Unknown)
                    {
                        finishReason = MapFinishReason(update.FinishReason.Value);
                    }

                    foreach (var toolCallUpdate in update.ToolCallUpdates)
                    {
                        if (!string.IsNullOrEmpty(toolCallUpdate.ToolCallId)
                            || !string.IsNullOrEmpty(toolCallUpdate.FunctionName))
                        {
                            yield return new LlmToolCallOpenedEvent(
                                toolCallUpdate.Index,
                                toolCallUpdate.ToolCallId,
                                toolCallUpdate.FunctionName
                                );
                        }

                        var argumentsUpdate = toolCallUpdate.FunctionArgumentsUpdate;
                        if (argumentsUpdate is not null && argumentsUpdate.ToMemory().Length > 0)
                        {
                            yield return new LlmToolCallArgumentsDeltaEvent(
                                toolCallUpdate.Index,
                                argumentsUpdate.ToString()
                                );
                        }
                    }

                    foreach (var contentPart in update.ContentUpdate)
                    {
                        if (contentPart.Kind != ChatMessageContentPartKind.Text)
                        {
                            continue;
                        }
                        if (string.IsNullOrEmpty(contentPart.Text))
                        {
                            continue;
                        }

                        yield return new LlmTextDeltaEvent(contentPart.Text);
                    }
                }
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            yield return new LlmFinishedEvent(finishReason);
        }

        /// <summary>
        /// The SDK client for one request. A client is cheap and carries no state worth keeping
        /// between turns, and the model may change from turn to turn when the user switches agent.
        /// </summary>
        private ChatClient CreateChatClient(
            string model
            )
        {
            var options = new OpenAIClientOptions
            {
                NetworkTimeout = NetworkTimeout,
                Endpoint = _endpoint,
            };

            if (_pipelineTransport is not null)
            {
                options.Transport = _pipelineTransport;
            }

            return new ChatClient(
                model: model,
                new ApiKeyCredential(_token),
                options
                );
        }

        /// <summary>Translates the SDK's failure into the neutral one, keeping the body the provider sent.</summary>
        private static LlmTransportException ToTransportException(
            ClientResultException excp
            )
        {
            string? body = null;
            try
            {
                body = excp.GetRawResponse()?.Content?.ToString();
            }
            catch (Exception)
            {
                //a response which has already been consumed keeps nothing to read
            }

            return new LlmTransportException(
                excp.Message,
                ServerErrorMessageReader.Read(body),
                excp.Status,
                excp
                );
        }

        /// <summary>Maps this protocol's finish reasons onto the neutral ones.</summary>
        private static LlmFinishReason MapFinishReason(
            ChatFinishReason reason
            )
        {
            if (reason == ChatFinishReason.ToolCalls || reason == ChatFinishReason.FunctionCall)
            {
                return LlmFinishReason.ToolCalls;
            }
            if (reason == ChatFinishReason.Stop)
            {
                return LlmFinishReason.Stop;
            }
            if (reason == ChatFinishReason.Length)
            {
                return LlmFinishReason.Length;
            }
            if (reason == ChatFinishReason.ContentFilter)
            {
                return LlmFinishReason.ContentFilter;
            }

            return LlmFinishReason.Unknown;
        }
    }
}
