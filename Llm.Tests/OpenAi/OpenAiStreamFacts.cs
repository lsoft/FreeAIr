using FreeAIr.Llm.OpenAi;
using FreeAIr.Llm.Tests.Fakes;
using System.ClientModel.Primitives;
using System.Net;
using Xunit;

namespace FreeAIr.Llm.Tests.OpenAi;

/// <summary>
/// What FreeAIr makes of the bytes an OpenAI compatible endpoint streams back.
///
/// The interesting cases are all failures of the naive reading: a tool call arrives in pieces, an
/// argument-less call arrives as nothing at all, and an endpoint which fell over answers with
/// something that is not a completion while still looking like a stream.
/// </summary>
public sealed class OpenAiStreamFacts
{
    [Fact]
    public async Task TextArrivesInOrderAndTheTurnEndsWithStop()
    {
        var result = await ReadAsync(
            Sse(
                """{"id":"c1","choices":[{"index":0,"delta":{"content":"Hello"}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{"content":", world"}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}"""
                )
            );

        Assert.Equal("Hello, world", result.Text);
        Assert.Equal(LlmFinishReason.Stop, result.FinishReason);
        Assert.Empty(result.ToolCalls);
    }

    [Fact]
    public async Task AToolCallSplitAcrossChunksIsPutBackTogether()
    {
        //the protocol only promises that the fragments of one call share their index: the id and
        //the name come once, the arguments as a series of deltas. Taking a single fragment yields a
        //tool which runs without arguments and a broken transcript which LM Studio answers with an
        //Internal Server Error on the next turn
        var result = await ReadAsync(
            Sse(
                """{"id":"c1","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"id":"call_1","type":"function","function":{"name":"VS.SearchFileContent","arguments":""}}]}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"{\"que"}}]}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"function":{"arguments":"ry\":\"ILlmTransport\"}"}}]}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{},"finish_reason":"tool_calls"}]}"""
                )
            );

        var toolCall = Assert.Single(result.ToolCalls);

        Assert.Equal("call_1", toolCall.Id);
        Assert.Equal("VS.SearchFileContent", toolCall.Name);
        Assert.Equal("""{"query":"ILlmTransport"}""", toolCall.ArgumentsJson);
        Assert.Equal(LlmFinishReason.ToolCalls, result.FinishReason);
    }

    [Fact]
    public async Task SeveralToolCallsKeepTheOrderTheModelOpenedThemIn()
    {
        var result = await ReadAsync(
            Sse(
                """{"id":"c1","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"id":"call_1","type":"function","function":{"name":"VS.Build","arguments":"{}"}}]}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{"tool_calls":[{"index":1,"id":"call_2","type":"function","function":{"name":"VS.GetErrorList","arguments":"{}"}}]}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{},"finish_reason":"tool_calls"}]}"""
                )
            );

        Assert.Equal(
            new[] { "VS.Build", "VS.GetErrorList" },
            result.ToolCalls.Select(c => c.Name).ToArray()
            );
    }

    [Fact]
    public async Task ACallWithoutArgumentsStillCarriesAJsonObject()
    {
        //an empty string is not a json object, and the endpoints reject the transcript containing it
        var result = await ReadAsync(
            Sse(
                """{"id":"c1","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"id":"call_1","type":"function","function":{"name":"VS.GetSolutionTree","arguments":""}}]}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{},"finish_reason":"tool_calls"}]}"""
                )
            );

        Assert.Equal("{}", Assert.Single(result.ToolCalls).ArgumentsJson);
    }

    [Fact]
    public async Task ReasoningContentArrivesAsReasoningAndNotAsAnswerText()
    {
        //DeepSeek, vLLM and llama.cpp put the thinking of a reasoning model in `reasoning_content`.
        //The OpenAI SDK has no property for it and drops it while parsing, so it is picked out of
        //the bytes separately - which is the only reason any of this is testable at all
        var result = await ReadAsync(
            Sse(
                """{"id":"c1","choices":[{"index":0,"delta":{"reasoning_content":"The user asks "}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{"reasoning_content":"for a number."}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{"content":"42"}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}"""
                )
            );

        Assert.Equal("42", result.Text);
        Assert.Equal("The user asks for a number.", result.Reasoning);
        Assert.Equal(LlmFinishReason.Stop, result.FinishReason);
    }

    [Fact]
    public async Task OpenRouterSpellsTheSameFieldReasoning()
    {
        //the field was never part of the protocol, so the gateways disagree about its name; an
        //agent pointed at openrouter.ai would otherwise show nothing at all
        var result = await ReadAsync(
            Sse(
                """{"id":"c1","choices":[{"index":0,"delta":{"reasoning":"Let me check."}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{"content":"ok"}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}"""
                )
            );

        Assert.Equal("ok", result.Text);
        Assert.Equal("Let me check.", result.Reasoning);
    }

    [Fact]
    public async Task AReasoningFieldWhichIsNullIsNotAnEvent()
    {
        //most servers send the key on every chunk and leave it null once the thinking is over; a
        //chat which believed that would open a think block around every answer
        var result = await ReadAsync(
            Sse(
                """{"id":"c1","choices":[{"index":0,"delta":{"reasoning_content":null,"content":"plain"}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{},"finish_reason":"stop"}]}"""
                )
            );

        Assert.Equal("plain", result.Text);
        Assert.Empty(result.Reasoning);
        Assert.DoesNotContain(result.Events, e => e is LlmReasoningDeltaEvent);
    }

    [Fact]
    public async Task ReasoningWhichEndsTheTurnInAToolCallIsNotLost()
    {
        //nothing is said and the last fragment sits behind the final chunk of the stream, so a
        //transport which only drained what it had seen so far would swallow it
        var result = await ReadAsync(
            Sse(
                """{"id":"c1","choices":[{"index":0,"delta":{"reasoning_content":"I should build."}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{"tool_calls":[{"index":0,"id":"call_1","type":"function","function":{"name":"VS.Build","arguments":"{}"}}]}}]}""",
                """{"id":"c1","choices":[{"index":0,"delta":{},"finish_reason":"tool_calls"}]}"""
                )
            );

        Assert.Equal("I should build.", result.Reasoning);
        Assert.Equal("VS.Build", Assert.Single(result.ToolCalls).Name);
        Assert.Equal(LlmFinishReason.ToolCalls, result.FinishReason);
    }

    [Fact]
    public async Task AChunkWhichIsNotACompletionEndsTheReadAsAFault()
    {
        //an error page or a quota message can still arrive shaped like a stream; without an id
        //there is nothing to read further, and the chat has to fail rather than wait
        var result = await ReadAsync(
            Sse(
                """{"choices":[{"index":0,"delta":{"content":"nonsense"}}]}"""
                )
            );

        Assert.Single(result.Faults);
        Assert.DoesNotContain(result.Events, e => e is LlmFinishedEvent);
    }

    [Fact]
    public async Task AFailedRequestCarriesTheSentenceTheServerSent()
    {
        //the exception's own message is only the status line; the parameter the server did not like
        //is named in the body and nowhere else
        var handler = CannedHttpHandler.Failure(
            HttpStatusCode.BadRequest,
            """{"error":{"message":"max_completion_tokens is too large for this model","type":"invalid_request_error"}}"""
            );

        var transport = new OpenAiChatTransport(
            new Uri("http://localhost:1234/v1"),
            "test-token",
            new HttpClientPipelineTransport(new HttpClient(handler))
            );

        var excp = await Assert.ThrowsAsync<LlmTransportException>(
            () => StreamEventCollector.CollectAsync(
                transport,
                new LlmRequest("gpt-4o", null, new[] { LlmMessage.CreateUserMessage("hi") })
                )
            );

        Assert.Equal("max_completion_tokens is too large for this model", excp.ServerMessage);
        Assert.Equal(400, excp.StatusCode);
    }

    /// <summary>Wraps the given chunks as a `text/event-stream` body, terminated the way the protocol requires.</summary>
    private static string Sse(
        params string[] chunks
        )
    {
        return string.Concat(chunks.Select(c => "data: " + c + "\n\n")) + "data: [DONE]\n\n";
    }

    private static Task<StreamEventCollector.Result> ReadAsync(
        string sse
        )
    {
        var transport = new OpenAiChatTransport(
            new Uri("http://localhost:1234/v1"),
            "test-token",
            new HttpClientPipelineTransport(new HttpClient(CannedHttpHandler.ServerSentEvents(sse)))
            );

        return StreamEventCollector.CollectAsync(
            transport,
            new LlmRequest("gpt-4o", null, new[] { LlmMessage.CreateUserMessage("hi") })
            );
    }
}
