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
