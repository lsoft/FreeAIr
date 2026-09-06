using FreeAIr.Llm.Anthropic;
using FreeAIr.Llm.OpenAi;
using FreeAIr.Llm.Tests.Fakes;
using FreeAIr.Llm.Wire;
using System.ClientModel.Primitives;
using System.Net;
using Xunit;

namespace FreeAIr.Llm.Tests;

/// <summary>
/// What reaches the log when a request goes out wrong but does not fail.
///
/// Every case here is a recovery: the request is still sent and the answer still arrives, so
/// nothing throws and nothing appears in the chat. They are also the likeliest causes of a
/// tester's report that "the tool ran with nothing in it" or "it just stopped", and without a line
/// in the activity log there is no way back from such a report to the cause.
/// </summary>
public sealed class DiagnosticsFacts
{
    private const string AnthropicEmptyStream =
        "event: message_start\ndata: {\"type\":\"message_start\",\"message\":{\"id\":\"msg_1\",\"role\":\"assistant\",\"content\":[]}}\n\n" +
        "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n";

    [Fact]
    public void AToolSchemaWhichHadToBeThrownAwayIsReported()
    {
        using var diagnostics = new CapturedDiagnostics();

        Assert.Equal(ToolSchemaNormalizer.NoParameters, ToolSchemaNormalizer.Normalize("not json at all"));

        Assert.True(diagnostics.Mentions("does not parse"), diagnostics.ToString());
    }

    [Fact]
    public void ASchemaWhichIsNotAnObjectIsReported()
    {
        using var diagnostics = new CapturedDiagnostics();

        Assert.Equal(ToolSchemaNormalizer.NoParameters, ToolSchemaNormalizer.Normalize("[1,2,3]"));

        Assert.True(diagnostics.Mentions("rather than an object"), diagnostics.ToString());
    }

    [Fact]
    public void AWellFormedSchemaIsNotReported()
    {
        using var diagnostics = new CapturedDiagnostics();

        ToolSchemaNormalizer.Normalize("""{"type":"object","properties":{"q":{"type":"string"}}}""");

        Assert.Empty(diagnostics.Reports);
    }

    [Fact]
    public async Task ArgumentsWhichHadToBeSentAsAnEmptyObjectAreReported()
    {
        //this is what "the model called the tool but it did nothing" looks like from the inside
        using var diagnostics = new CapturedDiagnostics();

        await SendToAnthropicAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: null,
                messages: new[]
                {
                    LlmMessage.CreateUserMessage("build it"),
                    LlmMessage.CreateAssistantToolCallMessage(
                        new[] { new LlmToolCall("toolu_1", "VS.Build", "{\"configuration\": ") }
                        ),
                    LlmMessage.CreateToolResultMessage(new LlmToolResult("toolu_1", "done")),
                }
                )
            );

        Assert.True(diagnostics.Mentions("toolu_1"), diagnostics.ToString());
        Assert.True(diagnostics.Mentions("VS.Build"), diagnostics.ToString());
    }

    [Fact]
    public async Task AToolResultDroppedFromTheRequestIsReported()
    {
        //the model never learns what the tool answered, and nothing else says so
        using var diagnostics = new CapturedDiagnostics();

        await SendToAnthropicAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: null,
                messages: new[]
                {
                    LlmMessage.CreateUserMessage("build it"),
                    LlmMessage.CreateToolResultMessage(new LlmToolResult("toolu_orphan", "done")),
                }
                )
            );

        Assert.True(diagnostics.Mentions("toolu_orphan"), diagnostics.ToString());
        Assert.True(diagnostics.Mentions("dropped"), diagnostics.ToString());
    }

    [Fact]
    public async Task AnOpenAiReplyWhichIsNotACompletionIsReported()
    {
        using var diagnostics = new CapturedDiagnostics();

        var transport = new OpenAiChatTransport(
            new Uri("http://localhost:1234/v1"),
            "test-token",
            new HttpClientPipelineTransport(
                new HttpClient(
                    CannedHttpHandler.ServerSentEvents(
                        "data: {\"choices\":[{\"index\":0,\"delta\":{\"content\":\"nonsense\"}}]}\n\ndata: [DONE]\n\n"
                        )
                    )
                )
            );

        await StreamEventCollector.CollectAsync(
            transport,
            new LlmRequest("gpt-4o", null, new[] { LlmMessage.CreateUserMessage("hi") })
            );

        Assert.True(diagnostics.Mentions("not a completion"), diagnostics.ToString());
        //the endpoint and the model are the first thing worth knowing from a report
        Assert.True(diagnostics.Mentions("localhost:1234"), diagnostics.ToString());
        Assert.True(diagnostics.Mentions("gpt-4o"), diagnostics.ToString());
    }

    [Fact]
    public async Task AnAnthropicStreamEventWhichIsNotJsonIsReported()
    {
        using var diagnostics = new CapturedDiagnostics();

        var transport = new AnthropicMessagesTransport(
            new Uri("https://api.anthropic.com"),
            "test-token",
            CannedHttpHandler.ServerSentEvents(
                "event: message_start\ndata: <html>a proxy said something</html>\n\n" +
                "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n"
                )
            );

        await StreamEventCollector.CollectAsync(
            transport,
            new LlmRequest("claude-opus-5", null, new[] { LlmMessage.CreateUserMessage("hi") })
            );

        Assert.True(diagnostics.Mentions("not valid json"), diagnostics.ToString());
        Assert.True(diagnostics.Mentions("a proxy said something"), diagnostics.ToString());
    }

    [Theory]
    [InlineData(LlmProtocol.OpenAi)]
    [InlineData(LlmProtocol.Anthropic)]
    public async Task ARefusedRequestNamesTheProtocolTheEndpointAndTheModel(
        LlmProtocol protocol
        )
    {
        //an agent pointed at one protocol while set to speak the other is the likeliest cause of a
        //404 or a 400, and none of that triple appears in an HTTP status line
        var handler = CannedHttpHandler.Failure(
            HttpStatusCode.NotFound,
            """{"error":{"message":"unknown route"}}"""
            );

        var endpoint = new Uri("https://example.invalid/v1");

        ILlmTransport transport = protocol == LlmProtocol.Anthropic
            ? new AnthropicMessagesTransport(endpoint, "test-token", handler)
            : new OpenAiChatTransport(endpoint, "test-token", new HttpClientPipelineTransport(new HttpClient(handler)))
            ;

        var excp = await Assert.ThrowsAsync<LlmTransportException>(
            () => StreamEventCollector.CollectAsync(
                transport,
                new LlmRequest("some-model", null, new[] { LlmMessage.CreateUserMessage("hi") })
                )
            );

        Assert.Contains(protocol.ToString(), excp.Message);
        Assert.Contains("example.invalid", excp.Message);
        Assert.Contains("some-model", excp.Message);
        Assert.Contains("404", excp.Message);
        //the server's own sentence stays apart, so the chat can print the two without repeating
        Assert.Equal("unknown route", excp.ServerMessage);
    }

    [Fact]
    public void ReportingWithNoSinkAttachedDoesNothingAndThrowsNothing()
    {
        var previous = LlmDiagnostics.Sink;
        try
        {
            LlmDiagnostics.Sink = null;

            LlmDiagnostics.Report("nobody is listening");
            LlmDiagnostics.Report("nobody is listening", new InvalidOperationException("either"));
        }
        finally
        {
            LlmDiagnostics.Sink = previous;
        }
    }

    [Fact]
    public void ABrokenSinkDoesNotTakeTheTurnDownWithIt()
    {
        var previous = LlmDiagnostics.Sink;
        try
        {
            LlmDiagnostics.Sink = _ => throw new InvalidOperationException("the log is on fire");

            LlmDiagnostics.Report("something worth knowing");
        }
        finally
        {
            LlmDiagnostics.Sink = previous;
        }
    }

    private static Task SendToAnthropicAsync(
        LlmRequest request
        )
    {
        var transport = new AnthropicMessagesTransport(
            new Uri("https://api.anthropic.com"),
            "test-token",
            CannedHttpHandler.ServerSentEvents(AnthropicEmptyStream)
            );

        return StreamEventCollector.CollectAsync(transport, request);
    }
}
