using FreeAIr.Helper;
using Microsoft.VisualStudio.Threading;
using OpenAI.Chat;
using System.ClientModel;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FreeAIr.Chat;
using FreeAIr.Chat.Content;

namespace FreeAIr.BLogic.Reader
{
    /// <summary>
    /// Performs one streaming completion request for a chat and pushes the result back into it.
    ///
    /// A reader serves exactly one chat and only one request at a time: <see cref="AsyncStartRead"/>
    /// does nothing while the previous read is still running. The reader never throws to its
    /// caller — every failure is turned into a chat answer plus a `Failed` chat status.
    ///
    /// Use <see cref="LLMReaderPool"/> to get one; do not create readers directly.
    /// </summary>
    public sealed class LLMReader : IDisposable
    {
        /// <summary>Guards <see cref="_task"/> and <see cref="_cancellationTokenSource"/> against concurrent start/stop calls.</summary>
        private readonly object _taskLocker = new();

        /// <summary>The chat this reader streams completions into.</summary>
        private readonly FreeAIr.Chat.Chat _chat;

        /// <summary>
        /// The source the read in flight was started with. Replaced by every
        /// <see cref="StopSafelyAsync"/>, so its identity also tells a read whether it is still the
        /// current one. Guarded by <see cref="_taskLocker"/>.
        /// </summary>
        private CancellationTokenSource _cancellationTokenSource = new();

        /// <summary>
        /// The read in flight, or null when the reader is idle. Guarded by <see cref="_taskLocker"/>.
        /// </summary>
        private Task? _task;

        /// <summary>
        /// How much of a non-JSON error page is kept in the chat. The useful sentence is at the
        /// front; the rest of a gateway HTML dump only buries it.
        /// </summary>
        private const int MaxReportedBodyLength = 4000;

        /// <summary>Creates a reader bound to the given chat; obtain one through <see cref="LLMReaderPool"/> instead of calling this directly.</summary>
        public LLMReader(
            FreeAIr.Chat.Chat chat
            )
        {
            if (chat is null)
            {
                throw new ArgumentNullException(nameof(chat));
            }

            _chat = chat;
        }

        /// <summary>
        /// Starts reading in the background and returns at once.
        /// Silently ignored if a read is already in progress.
        /// </summary>
        public void AsyncStartRead(
            )
        {
            lock (_taskLocker)
            {
                if (_task is not null)
                {
                    return;
                }

                //the source is taken here, under the lock, and not somewhere inside the read:
                //a concurrent StopSafelyAsync replaces the field, and the read which is being
                //started right now must not pick up the source that stop has just cancelled
                _task = ReadSafelyAsync(_cancellationTokenSource);
            }
        }

        /// <summary>Awaits the read currently in flight, if any; returns immediately when the reader is idle.</summary>
        public async Task WaitForTaskAsync(
            )
        {
            Task? task = null;
            lock (_taskLocker)
            {
                task = _task;
            }

            if (task is null)
            {
                return;
            }

            await task;
        }

        /// <summary>
        /// Cancels the read in flight and waits for it to unwind. Never throws.
        ///
        /// The fresh cancellation source is armed before the wait, not after it: the reader is
        /// public and a new read may be requested the very moment the old one has been let go, and
        /// that read has to start with a source nobody has cancelled.
        /// </summary>
        public async Task StopSafelyAsync()
        {
            try
            {
                Task? task;
                CancellationTokenSource cancellationTokenSource;
                lock (_taskLocker)
                {
                    task = _task;
                    _task = null;

                    cancellationTokenSource = _cancellationTokenSource;
                    _cancellationTokenSource = new CancellationTokenSource();
                }

                if (task is null)
                {
                    //nothing was reading, so nobody has ever seen the source we have just replaced
                    cancellationTokenSource.Dispose();
                    return;
                }

                cancellationTokenSource.Cancel();

                try
                {
                    await task;
                }
                finally
                {
                    //only now, when the read which held the token has unwound, the source is free
                    cancellationTokenSource.Dispose();
                }
            }
            catch (Exception excp)
            {
                excp.ActivityLogException();
            }
        }

        /// <summary>Releases the current cancellation token source.</summary>
        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }

        /// <summary>Runs one read and, when it finishes, clears <see cref="_task"/> unless a newer read has already replaced the cancellation source.</summary>
        private async Task ReadSafelyAsync(
            CancellationTokenSource cancellationTokenSource
            )
        {
            try
            {
                await ReadSafelyPrivateAsync(cancellationTokenSource.Token);
            }
            finally
            {
                lock (_taskLocker)
                {
                    //if the reader has been stopped and started again while this read was
                    //unwinding, _task belongs to that newer read and must be left alone
                    if (ReferenceEquals(_cancellationTokenSource, cancellationTokenSource))
                    {
                        _task = null;
                    }
                }
            }
        }

        /// <summary>
        /// The body of a single turn:
        /// build the request from the chat, stream the completion, append the text to the answer
        /// as it arrives (so the UI updates live) and register the tool calls the model asked for.
        ///
        /// The tools are NOT invoked here. Each <see cref="ToolCallChatContent"/> executes itself
        /// (possibly after asking the user for a permission), and the last one to finish restarts
        /// this reader through <see cref="FreeAIr.Chat.Chat.CreateToolCall"/>.
        /// </summary>
        private async Task ReadSafelyPrivateAsync(
            CancellationToken cancellationToken
            )
        {
            //never block the UI thread while streaming
            await TaskScheduler.Default;

            AnswerChatContent? chatAnswer = null;

            try
            {
                _chat.Status = ChatStatusEnum.WaitingForAnswer;

                var chatClient = _chat.CreateChatClient();
                var chatCompletionOptions = await _chat.CreateChatCompletionOptionsAsync();



                var messages = await _chat.GetMessageListAsync();

                var completionUpdates = chatClient.CompleteChatStreaming(
                    messages: messages,
                    options: chatCompletionOptions,
                    cancellationToken: cancellationToken
                    );

                OpenAI.Chat.ChatFinishReason? chatFinishReason = null;
                var toolCallAccumulator = new StreamingToolCallAccumulator();
                //var contentParts = new List<ChatMessageContentPart>();

                _chat.Status = ChatStatusEnum.ReadingAnswer;

                foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
                {
                    //completionUpdate.Usage.OutputTokenDetails.

                    //a chunk without a completion id means the endpoint replied with
                    //something which is not a completion at all (an error page, a quota
                    //message, etc.); there is nothing to read further
                    if (completionUpdate.CompletionId is null)
                    {
                        chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, "Server returns error.");
                        _chat.Status = ChatStatusEnum.Failed;
                        return;
                    }

                    chatFinishReason ??= completionUpdate.FinishReason;
                    toolCallAccumulator.Append(completionUpdate.ToolCallUpdates);

                    //if (completionUpdate.FinishReason != ChatFinishReason.ToolCalls
                    //    || completionUpdate.ToolCallUpdates.Count == 0
                    //    )
                    //{
                    //    if (completionUpdate.ContentUpdate.Count > 0)
                    //    {
                    //        contentParts.AddRange(completionUpdate.ContentUpdate);
                    //    }
                    //}

                    foreach (ChatMessageContentPart contentPart in completionUpdate.ContentUpdate)
                    {
                        if (contentPart.Kind != ChatMessageContentPartKind.Text)
                        {
                            continue;
                        }
                        if (string.IsNullOrEmpty(contentPart.Text))
                        {
                            continue;
                        }

                        //for updating UI in real time
                        chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, contentPart.Text);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            _chat.Status = ChatStatusEnum.Ready;
                            return;
                        }
                    }
                }

                if (chatFinishReason == ChatFinishReason.ToolCalls)
                {
                    foreach (var toolCall in toolCallAccumulator.Build())
                    {
                        var toolCallContent = _chat.CreateToolCall(
                            toolCall
                            );


                        //var toolArguments = ParseToolInvocationArguments(toolCall);

                        //var toolResult = await McpServerProxyCollection.CallToolAsync(
                        //    toolCall.FunctionName,
                        //    toolArguments,
                        //    cancellationToken: CancellationToken.None
                        //    );
                        //if (toolResult is null)
                        //{
                        //    //dialog.AppendUnsuccessfulToolCall(
                        //    //    toolCall
                        //    //    );

                        //    throw new InvalidOperationException($"Tool named {toolCall.FunctionName} failed to run.");
                        //}

                        ////dialog.AppendToolCallResult(
                        ////    toolCall,
                        ////    toolResult
                        ////    );
                    }

                }





                _chat.Status = ChatStatusEnum.Ready;
            }
            catch (OperationCanceledException)
            {
                //this is OK
                _chat.Status = ChatStatusEnum.Ready;
            }
            catch (Exception excp)
            {
                chatAnswer = await CreateOrAppendAnswerPartAsync(chatAnswer, excp);

                _chat.Status = ChatStatusEnum.Failed;

                excp.ActivityLogException();
            }
        }

        /// <summary>
        /// Rebuilds whole tool calls out of the fragments a streaming completion delivers.
        ///
        /// An endpoint is free to announce the id and the name of a call in one chunk and to send
        /// its arguments as a series of deltas in the chunks that follow; the protocol only
        /// promises that the fragments of one call share their index. Taking a single fragment
        /// therefore yields a call whose arguments are empty or cut in half — the tool then runs
        /// without arguments, and the broken fragment is carried back to the server with the next
        /// request, which LM Studio answers with an Internal Server Error.
        /// </summary>
        private sealed class StreamingToolCallAccumulator
        {
            /// <summary>The tool call fragments seen so far, keyed by the index the streaming protocol assigns each call.</summary>
            private readonly Dictionary<int, ToolCallParts> _byIndex = new();

            /// <summary>
            /// The indices in the order the model opened them, so that the tool calls are offered
            /// to the user in the order they were asked for rather than in hash order.
            /// </summary>
            private readonly List<int> _order = new();

            /// <summary>Merges one streaming update's tool-call fragments into the accumulator, tracking each call by its index.</summary>
            public void Append(
                IReadOnlyList<StreamingChatToolCallUpdate> updates
                )
            {
                foreach (var update in updates)
                {
                    if (!_byIndex.TryGetValue(update.Index, out var parts))
                    {
                        parts = new ToolCallParts();
                        _byIndex.Add(update.Index, parts);
                        _order.Add(update.Index);
                    }

                    //everything but the arguments is sent once, by whichever chunk opens the call
                    if (!string.IsNullOrEmpty(update.ToolCallId))
                    {
                        parts.ToolCallId = update.ToolCallId;
                    }
                    if (!string.IsNullOrEmpty(update.FunctionName))
                    {
                        parts.FunctionName = update.FunctionName;
                        parts.Kind = update.Kind;
                    }

                    var argumentsUpdate = update.FunctionArgumentsUpdate;
                    if (argumentsUpdate is not null && argumentsUpdate.Length > 0)
                    {
                        parts.Arguments.Append(argumentsUpdate.ToString());
                    }
                }
            }

            /// <summary>
            /// The complete calls, in the order the model opened them. A fragment group which never
            /// received a name is not a call the chat could execute and is dropped here rather than
            /// passed on as a tool nobody can find.
            /// </summary>
            public IReadOnlyList<StreamingChatToolCallUpdate> Build()
            {
                var result = new List<StreamingChatToolCallUpdate>();

                foreach (var index in _order)
                {
                    var parts = _byIndex[index];
                    if (string.IsNullOrEmpty(parts.FunctionName))
                    {
                        continue;
                    }

                    result.Add(
                        OpenAIChatModelFactory.StreamingChatToolCallUpdate(
                            index: index,
                            toolCallId: parts.ToolCallId,
                            kind: parts.Kind,
                            functionName: parts.FunctionName,
                            functionArgumentsUpdate: BinaryData.FromString(
                                //a call without arguments still has to carry a json object:
                                //an empty string is not one, and the endpoints reject it
                                parts.Arguments.Length > 0
                                    ? parts.Arguments.ToString()
                                    : "{}"
                                )
                            )
                        );
                }

                return result;
            }

            /// <summary>The fragments collected so far for one tool call, before they are merged into a complete <see cref="StreamingChatToolCallUpdate"/>.</summary>
            private sealed class ToolCallParts
            {
                /// <summary>The tool call's id, sent once by the chunk that opens the call.</summary>
                public string? ToolCallId;

                /// <summary>The name of the tool being called, sent once by the chunk that opens the call.</summary>
                public string? FunctionName;

                /// <summary>The kind of tool call being made.</summary>
                public ChatToolCallKind Kind;

                /// <summary>The call's arguments, accumulated across the deltas the endpoint streams for this index.</summary>
                public readonly StringBuilder Arguments = new();
            }
        }

        /// <summary>
        /// Formats an exception as answer text and appends it to the chat, creating the answer
        /// content if this is the first piece. OpenAI.dll's message is only the HTTP status —
        /// vLLM names the actual fault (`max_completion_tokens`, the context window) in the
        /// body, so that body is shown too when it can still be read.
        /// </summary>
        private async Task<AnswerChatContent> CreateOrAppendAnswerPartAsync(
            AnswerChatContent? chatAnswer,
            Exception excp
            )
        {
            if (excp is null)
            {
                throw new ArgumentNullException(nameof(excp));
            }

            var answerPart = new StringBuilder();
            answerPart.AppendLine();
            answerPart.AppendLine(excp.Message);

            var serverMessage = TryReadServerErrorMessage(excp);
            if (!string.IsNullOrWhiteSpace(serverMessage)
                && !string.Equals(serverMessage, excp.Message, StringComparison.Ordinal))
            {
                answerPart.AppendLine();
                answerPart.AppendLine(serverMessage);
            }

            answerPart.AppendLine();
            answerPart.Append(excp.StackTrace);

            return await CreateOrAppendAnswerPartAsync(chatAnswer, answerPart.ToString());
        }

        /// <summary>
        /// The complaint the endpoint actually sent. `ClientResultException.Message` is
        /// `Service request failed. Status: 400`; the JSON underneath it is the only place
        /// that names the parameter the server did not like.
        /// </summary>
        private static string? TryReadServerErrorMessage(
            Exception excp
            )
        {
            for (var current = excp; current is not null; current = current.InnerException)
            {
                if (current is not ClientResultException clientException)
                {
                    continue;
                }

                string? body;
                try
                {
                    body = clientException.GetRawResponse()?.Content?.ToString();
                }
                catch (Exception)
                {
                    //a response which has already been consumed keeps nothing to read
                    continue;
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    continue;
                }

                var trimmed = body.Trim();
                var fromJson = TryReadJsonErrorMessage(trimmed);
                if (!string.IsNullOrWhiteSpace(fromJson))
                {
                    return fromJson;
                }

                if (trimmed.Length > MaxReportedBodyLength)
                {
                    return trimmed.Substring(0, MaxReportedBodyLength) + "...";
                }

                return trimmed;
            }

            return null;
        }

        /// <summary>
        /// Pulls `error.message` (OpenAI / vLLM) or a top-level `message` out of the response
        /// body so the chat shows the sentence the user can act on, not the whole JSON envelope.
        /// </summary>
        private static string? TryReadJsonErrorMessage(
            string body
            )
        {
            try
            {
                using var json = JsonDocument.Parse(body);
                if (json.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                if (json.RootElement.TryGetProperty("error", out var error)
                    && error.ValueKind == JsonValueKind.Object
                    && error.TryGetProperty("message", out var nested)
                    && nested.ValueKind == JsonValueKind.String)
                {
                    return nested.GetString();
                }

                if (json.RootElement.TryGetProperty("message", out var message)
                    && message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString();
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }


        /// <summary>Appends a piece of streamed text to the chat's answer, creating the answer content on the first call so the UI can update live.</summary>
        private async Task<AnswerChatContent> CreateOrAppendAnswerPartAsync(
            AnswerChatContent? chatAnswer,
            string answerPart
            )
        {
            if (chatAnswer is null)
            {
                chatAnswer = _chat.CreateAnswer();
            }

            await chatAnswer.AppendAsync(answerPart);

            return chatAnswer;
        }

    }
}
