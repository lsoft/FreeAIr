using FreeAIr.Llm.Anthropic;
using FreeAIr.Llm.Tests.Fakes;
using System.Net;
using Xunit;

namespace FreeAIr.Llm.Tests.Anthropic;

/// <summary>
/// What FreeAIr makes of an Anthropic answer.
///
/// The stream is a sequence of content blocks rather than a sequence of choices: text and tool
/// calls share one numbering, a tool call's arguments arrive as `input_json_delta` fragments which
/// are not valid JSON until the last one, and the reason the turn ended comes in its own event at
/// the end rather than riding along with the chunks.
/// </summary>
public sealed class AnthropicStreamFacts
{
    [Fact]
    public async Task TextArrivesInOrderAndTheTurnEndsWithStop()
    {
        var result = await ReadAsync(
            Sse(
                ("message_start", """{"type":"message_start","message":{"id":"msg_1","role":"assistant","content":[]}}"""),
                ("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}"""),
                ("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hello"}}"""),
                ("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":", world"}}"""),
                ("content_block_stop", """{"type":"content_block_stop","index":0}"""),
                ("message_delta", """{"type":"message_delta","delta":{"stop_reason":"end_turn","stop_sequence":null},"usage":{"output_tokens":15}}"""),
                ("message_stop", """{"type":"message_stop"}""")
                )
            );

        Assert.Equal("Hello, world", result.Text);
        Assert.Equal(LlmFinishReason.Stop, result.FinishReason);
        Assert.Empty(result.ToolCalls);
    }

    [Fact]
    public async Task AToolCallIsRebuiltOutOfItsInputJsonFragments()
    {
        var result = await ReadAsync(
            Sse(
                ("message_start", """{"type":"message_start","message":{"id":"msg_1","role":"assistant","content":[]}}"""),
                ("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}"""),
                ("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Let me look."}}"""),
                ("content_block_stop", """{"type":"content_block_stop","index":0}"""),
                ("content_block_start", """{"type":"content_block_start","index":1,"content_block":{"type":"tool_use","id":"toolu_01","name":"VS.SearchFileContent","input":{}}}"""),
                ("content_block_delta", """{"type":"content_block_delta","index":1,"delta":{"type":"input_json_delta","partial_json":""}}"""),
                ("content_block_delta", """{"type":"content_block_delta","index":1,"delta":{"type":"input_json_delta","partial_json":"{\"query\":"}}"""),
                ("content_block_delta", """{"type":"content_block_delta","index":1,"delta":{"type":"input_json_delta","partial_json":" \"ILlmTransport\"}"}}"""),
                ("content_block_stop", """{"type":"content_block_stop","index":1}"""),
                ("message_delta", """{"type":"message_delta","delta":{"stop_reason":"tool_use","stop_sequence":null},"usage":{"output_tokens":89}}"""),
                ("message_stop", """{"type":"message_stop"}""")
                )
            );

        Assert.Equal("Let me look.", result.Text);
        Assert.Equal(LlmFinishReason.ToolCalls, result.FinishReason);

        var toolCall = Assert.Single(result.ToolCalls);
        Assert.Equal("toolu_01", toolCall.Id);
        Assert.Equal("VS.SearchFileContent", toolCall.Name);
        Assert.Equal("""{"query": "ILlmTransport"}""", toolCall.ArgumentsJson);
    }

    [Fact]
    public async Task SeveralToolCallsKeepTheOrderTheModelOpenedThemIn()
    {
        var result = await ReadAsync(
            Sse(
                ("message_start", """{"type":"message_start","message":{"id":"msg_1","role":"assistant","content":[]}}"""),
                ("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"toolu_01","name":"VS.Build","input":{}}}"""),
                ("content_block_stop", """{"type":"content_block_stop","index":0}"""),
                ("content_block_start", """{"type":"content_block_start","index":1,"content_block":{"type":"tool_use","id":"toolu_02","name":"VS.GetErrorList","input":{}}}"""),
                ("content_block_stop", """{"type":"content_block_stop","index":1}"""),
                ("message_delta", """{"type":"message_delta","delta":{"stop_reason":"tool_use","stop_sequence":null}}"""),
                ("message_stop", """{"type":"message_stop"}""")
                )
            );

        Assert.Equal(
            new[] { "VS.Build", "VS.GetErrorList" },
            result.ToolCalls.Select(c => c.Name).ToArray()
            );
        //a call which was streamed without a single input fragment still has to carry an object
        Assert.All(result.ToolCalls, c => Assert.Equal("{}", c.ArgumentsJson));
    }

    [Fact]
    public async Task ThinkingIsNotPartOfTheAnswerText()
    {
        //the reasoning of an extended-thinking model is not what the chat window shows, and every
        //caller which parses an answer would have to strip it again
        var result = await ReadAsync(
            Sse(
                ("message_start", """{"type":"message_start","message":{"id":"msg_1","role":"assistant","content":[]}}"""),
                ("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"thinking","thinking":"","signature":""}}"""),
                ("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"thinking_delta","thinking":"The user wants..."}}"""),
                ("content_block_stop", """{"type":"content_block_stop","index":0}"""),
                ("content_block_start", """{"type":"content_block_start","index":1,"content_block":{"type":"text","text":""}}"""),
                ("content_block_delta", """{"type":"content_block_delta","index":1,"delta":{"type":"text_delta","text":"42"}}"""),
                ("content_block_stop", """{"type":"content_block_stop","index":1}"""),
                ("message_delta", """{"type":"message_delta","delta":{"stop_reason":"end_turn"}}"""),
                ("message_stop", """{"type":"message_stop"}""")
                )
            );

        Assert.Equal("42", result.Text);
    }

    [Fact]
    public async Task AnAnswerCutOffByTheTokenLimitSaysSo()
    {
        var result = await ReadAsync(
            Sse(
                ("message_start", """{"type":"message_start","message":{"id":"msg_1","role":"assistant","content":[]}}"""),
                ("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}"""),
                ("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"It began"}}"""),
                ("message_delta", """{"type":"message_delta","delta":{"stop_reason":"max_tokens"}}"""),
                ("message_stop", """{"type":"message_stop"}""")
                )
            );

        Assert.Equal(LlmFinishReason.Length, result.FinishReason);
    }

    [Fact]
    public async Task PingEventsAreIgnored()
    {
        var result = await ReadAsync(
            Sse(
                ("message_start", """{"type":"message_start","message":{"id":"msg_1","role":"assistant","content":[]}}"""),
                ("ping", """{"type":"ping"}"""),
                ("content_block_start", """{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}"""),
                ("content_block_delta", """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"ok"}}"""),
                ("ping", """{"type":"ping"}"""),
                ("message_delta", """{"type":"message_delta","delta":{"stop_reason":"end_turn"}}"""),
                ("message_stop", """{"type":"message_stop"}""")
                )
            );

        Assert.Equal("ok", result.Text);
        Assert.Equal(LlmFinishReason.Stop, result.FinishReason);
    }

    [Fact]
    public async Task AnErrorSentMidStreamEndsTheReadAsAFault()
    {
        //the api sends these while a 200 response is already in flight - an overloaded_error is the
        //usual one - so there is no status code to notice and the chat would otherwise sit waiting
        var result = await ReadAsync(
            Sse(
                ("message_start", """{"type":"message_start","message":{"id":"msg_1","role":"assistant","content":[]}}"""),
                ("error", """{"type":"error","error":{"type":"overloaded_error","message":"Overloaded"}}""")
                )
            );

        Assert.Contains("Overloaded", Assert.Single(result.Faults));
        Assert.DoesNotContain(result.Events, e => e is LlmFinishedEvent);
    }

    [Fact]
    public async Task AFailedRequestCarriesTheSentenceTheServerSent()
    {
        var handler = CannedHttpHandler.Failure(
            HttpStatusCode.BadRequest,
            """{"type":"error","error":{"type":"invalid_request_error","message":"max_tokens: field required"}}"""
            );

        var transport = new AnthropicMessagesTransport(
            new Uri("https://api.anthropic.com"),
            "test-token",
            handler
            );

        var excp = await Assert.ThrowsAsync<LlmTransportException>(
            () => StreamEventCollector.CollectAsync(
                transport,
                new LlmRequest("claude-opus-5", null, new[] { LlmMessage.CreateUserMessage("hi") })
                )
            );

        Assert.Equal("max_tokens: field required", excp.ServerMessage);
        Assert.Equal(400, excp.StatusCode);
    }

    /// <summary>Wraps named events as the `text/event-stream` body the API sends.</summary>
    private static string Sse(
        params (string Name, string Data)[] events
        )
    {
        return string.Concat(
            events.Select(e => "event: " + e.Name + "\ndata: " + e.Data + "\n\n")
            );
    }

    private static Task<StreamEventCollector.Result> ReadAsync(
        string sse
        )
    {
        var transport = new AnthropicMessagesTransport(
            new Uri("https://api.anthropic.com"),
            "test-token",
            CannedHttpHandler.ServerSentEvents(sse)
            );

        return StreamEventCollector.CollectAsync(
            transport,
            new LlmRequest("claude-opus-5", null, new[] { LlmMessage.CreateUserMessage("hi") })
            );
    }
}
