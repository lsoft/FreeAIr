using FreeAIr.Llm.Anthropic;
using FreeAIr.Llm.Tests.Fakes;
using System.Text.Json;
using Xunit;

namespace FreeAIr.Llm.Tests.Anthropic;

/// <summary>
/// What FreeAIr has to put on the wire to be understood by the Anthropic messages API.
///
/// Almost none of it is a rename of the OpenAI request. The system prompt is a field rather than a
/// message, `max_tokens` is mandatory, a tool's schema is `input_schema`, arguments travel as a
/// JSON object rather than as a string, and a tool's answer is a block inside a *user* message
/// because this protocol has no tool role at all.
/// </summary>
public sealed class AnthropicRequestFacts
{
    /// <summary>Enough of a stream to let a request finish; the tests here look at what was sent.</summary>
    private const string EmptyStream =
        "event: message_start\ndata: {\"type\":\"message_start\",\"message\":{\"id\":\"msg_1\",\"role\":\"assistant\",\"content\":[]}}\n\n" +
        "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n";

    [Fact]
    public async Task TheRequestGoesToTheMessagesEndpointWithTheProtocolHeaders()
    {
        var handler = await SendAsync(SimpleRequest());

        Assert.Equal(
            "https://api.anthropic.com/v1/messages",
            handler.LastRequest!.RequestUri!.ToString()
            );
        Assert.Equal(
            "test-token",
            Assert.Single(handler.LastRequest.Headers.GetValues("x-api-key"))
            );
        Assert.Equal(
            AnthropicMessagesTransport.AnthropicVersion,
            Assert.Single(handler.LastRequest.Headers.GetValues("anthropic-version"))
            );
        Assert.True(JsonDocument.Parse(handler.LastRequestBody).RootElement.GetProperty("stream").GetBoolean());
    }

    [Fact]
    public async Task AnEndpointWhichAlreadyNamesTheApiVersionIsNotGivenASecondOne()
    {
        //the agent editor's endpoint field is normally filled in with an OpenAI style base ending
        //in /v1, and a user switching an existing agent over will leave it that way
        var handler = await SendAsync(
            SimpleRequest(),
            endpoint: new Uri("https://api.anthropic.com/v1")
            );

        Assert.Equal(
            "https://api.anthropic.com/v1/messages",
            handler.LastRequest!.RequestUri!.ToString()
            );
    }

    [Fact]
    public async Task TheSystemPromptIsAFieldAndNotAMessage()
    {
        var body = await CaptureAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: "You are a helpful assistant.",
                messages: new[] { LlmMessage.CreateUserMessage("hi") }
                )
            );

        Assert.Equal("You are a helpful assistant.", body.RootElement.GetProperty("system").GetString());

        var messages = body.RootElement.GetProperty("messages");
        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
    }

    [Fact]
    public async Task NoSystemMemberIsWrittenWhenTheAgentHasNoPrompt()
    {
        var body = await CaptureAsync(SimpleRequest());

        Assert.False(body.RootElement.TryGetProperty("system", out _));
    }

    [Fact]
    public async Task MaxTokensIsAlwaysSentBecauseTheApiRequiresIt()
    {
        var configured = await CaptureAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: null,
                messages: new[] { LlmMessage.CreateUserMessage("hi") },
                maxOutputTokens: 4096
                )
            );

        Assert.Equal(4096, configured.RootElement.GetProperty("max_tokens").GetInt32());

        //unlike OpenAI, leaving it out is not an option: the request is rejected outright
        var unset = await CaptureAsync(SimpleRequest());

        Assert.Equal(
            AnthropicMessagesTransport.DefaultMaxOutputTokens,
            unset.RootElement.GetProperty("max_tokens").GetInt32()
            );
    }

    [Fact]
    public async Task AToolIsDeclaredWithAnInputSchema()
    {
        var body = await CaptureAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: null,
                messages: new[] { LlmMessage.CreateUserMessage("hi") },
                tools: new[]
                {
                    new LlmToolDefinition(
                        "VS.SearchFileContent",
                        "Searches the solution.",
                        """{"type":"object","properties":{"query":{"type":"string"}}}"""
                        ),
                }
                )
            );

        var tool = body.RootElement.GetProperty("tools")[0];

        Assert.Equal("VS.SearchFileContent", tool.GetProperty("name").GetString());
        Assert.Equal("Searches the solution.", tool.GetProperty("description").GetString());
        //`parameters` is the other protocol's name for it, and `strict` does not exist here
        Assert.False(tool.TryGetProperty("parameters", out _));
        Assert.False(tool.TryGetProperty("strict", out _));
        Assert.True(
            tool.GetProperty("input_schema").GetProperty("properties").TryGetProperty("query", out _)
            );
        Assert.Equal("auto", body.RootElement.GetProperty("tool_choice").GetProperty("type").GetString());
    }

    [Fact]
    public async Task AToolDeclaringNoParametersStillGetsAnObjectSchema()
    {
        var body = await CaptureAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: null,
                messages: new[] { LlmMessage.CreateUserMessage("hi") },
                tools: new[] { new LlmToolDefinition("VS.GetSolutionTree", "Reads the tree.", "{}") }
                )
            );

        var schema = body.RootElement.GetProperty("tools")[0].GetProperty("input_schema");

        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Object, schema.GetProperty("properties").ValueKind);
    }

    [Fact]
    public async Task NoToolsMeansNeitherToolsNorAToolChoice()
    {
        //an empty `tools` array is not the same as no tools here, and a tool_choice without tools
        //to choose from is a validation error
        var body = await CaptureAsync(SimpleRequest());

        Assert.False(body.RootElement.TryGetProperty("tools", out _));
        Assert.False(body.RootElement.TryGetProperty("tool_choice", out _));
    }

    [Fact]
    public async Task AToolCallBecomesAToolUseBlockWithItsArgumentsAsAnObject()
    {
        //the other protocol carries the arguments as a JSON *string*; here they are the real thing,
        //so a transport which passed the text through would send `input` as a quoted blob
        var body = await CaptureAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: null,
                messages: new[]
                {
                    LlmMessage.CreateUserMessage("build it"),
                    LlmMessage.CreateAssistantToolCallMessage(
                        new[] { new LlmToolCall("toolu_1", "VS.Build", """{"configuration":"Debug"}""") }
                        ),
                    LlmMessage.CreateToolResultMessage(new LlmToolResult("toolu_1", "Build succeeded.")),
                }
                )
            );

        var messages = body.RootElement.GetProperty("messages");

        var assistant = messages[1];
        Assert.Equal("assistant", assistant.GetProperty("role").GetString());
        var toolUse = assistant.GetProperty("content")[0];
        Assert.Equal("tool_use", toolUse.GetProperty("type").GetString());
        Assert.Equal("toolu_1", toolUse.GetProperty("id").GetString());
        Assert.Equal("VS.Build", toolUse.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Object, toolUse.GetProperty("input").ValueKind);
        Assert.Equal("Debug", toolUse.GetProperty("input").GetProperty("configuration").GetString());
    }

    [Fact]
    public async Task AToolResultIsABlockInsideAUserMessage()
    {
        //this protocol has no tool role at all
        var body = await CaptureAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: null,
                messages: new[]
                {
                    LlmMessage.CreateUserMessage("build it"),
                    LlmMessage.CreateAssistantToolCallMessage(
                        new[] { new LlmToolCall("toolu_1", "VS.Build", "{}") }
                        ),
                    LlmMessage.CreateToolResultMessage(new LlmToolResult("toolu_1", "Build succeeded.")),
                }
                )
            );

        var result = body.RootElement.GetProperty("messages")[2];

        Assert.Equal("user", result.GetProperty("role").GetString());
        var block = result.GetProperty("content")[0];
        Assert.Equal("tool_result", block.GetProperty("type").GetString());
        Assert.Equal("toolu_1", block.GetProperty("tool_use_id").GetString());
        Assert.Equal("Build succeeded.", block.GetProperty("content").GetString());
        Assert.False(block.TryGetProperty("is_error", out var isError) && isError.GetBoolean());
    }

    [Fact]
    public async Task AFailedToolIsReportedAsAnErrorResult()
    {
        var body = await CaptureAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: null,
                messages: new[]
                {
                    LlmMessage.CreateUserMessage("build it"),
                    LlmMessage.CreateAssistantToolCallMessage(
                        new[] { new LlmToolCall("toolu_1", "VS.Build", "{}") }
                        ),
                    LlmMessage.CreateToolResultMessage(
                        new LlmToolResult("toolu_1", "The user refused to run this tool.", isError: true)
                        ),
                }
                )
            );

        var block = body.RootElement.GetProperty("messages")[2].GetProperty("content")[0];

        Assert.True(block.GetProperty("is_error").GetBoolean());
    }

    [Fact]
    public async Task TwoToolsOfOneTurnTravelAsOneAssistantMessageAndOneUserMessage()
    {
        //the chat records a call and its answer as a pair, so two tools produce
        //assistant/result/assistant/result - which this protocol rejects: every tool_use of a turn
        //belongs to one assistant message, and every tool_result to the one user message that
        //answers it
        var body = await CaptureAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: null,
                messages: new[]
                {
                    LlmMessage.CreateUserMessage("check the build"),
                    LlmMessage.CreateAssistantToolCallMessage(
                        new[] { new LlmToolCall("toolu_1", "VS.Build", "{}") }
                        ),
                    LlmMessage.CreateToolResultMessage(new LlmToolResult("toolu_1", "Build failed.")),
                    LlmMessage.CreateAssistantToolCallMessage(
                        new[] { new LlmToolCall("toolu_2", "VS.GetErrorList", "{}") }
                        ),
                    LlmMessage.CreateToolResultMessage(new LlmToolResult("toolu_2", "CS0246 in Foo.cs")),
                }
                )
            );

        var messages = body.RootElement.GetProperty("messages");

        Assert.Equal(3, messages.GetArrayLength());

        Assert.Equal("user", messages[0].GetProperty("role").GetString());

        var assistant = messages[1];
        Assert.Equal("assistant", assistant.GetProperty("role").GetString());
        Assert.Equal(2, assistant.GetProperty("content").GetArrayLength());
        Assert.Equal("toolu_1", assistant.GetProperty("content")[0].GetProperty("id").GetString());
        Assert.Equal("toolu_2", assistant.GetProperty("content")[1].GetProperty("id").GetString());

        var results = messages[2];
        Assert.Equal("user", results.GetProperty("role").GetString());
        Assert.Equal(2, results.GetProperty("content").GetArrayLength());
        Assert.Equal("toolu_1", results.GetProperty("content")[0].GetProperty("tool_use_id").GetString());
        Assert.Equal("toolu_2", results.GetProperty("content")[1].GetProperty("tool_use_id").GetString());
    }

    [Fact]
    public async Task ConsecutiveUserMessagesAreMergedIntoOne()
    {
        //every chat sends the context documents as user messages right before the prompt, so a run
        //of them is the normal case rather than an edge one
        var body = await CaptureAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: null,
                messages: new[]
                {
                    LlmMessage.CreateUserMessage("Here is Program.cs"),
                    LlmMessage.CreateUserMessage("Here is Startup.cs"),
                    LlmMessage.CreateUserMessage("What does this do?"),
                }
                )
            );

        var messages = body.RootElement.GetProperty("messages");

        var single = Assert.Single(messages.EnumerateArray());
        Assert.Equal("user", single.GetProperty("role").GetString());
        Assert.Equal(3, single.GetProperty("content").GetArrayLength());
        Assert.Equal("Here is Program.cs", single.GetProperty("content")[0].GetProperty("text").GetString());
    }

    [Fact]
    public async Task AConversationWhichWouldStartWithTheAssistantIsTrimmedToStartWithTheUser()
    {
        //the api rejects a first message which is not the user's, and archiving can leave the
        //transcript starting anywhere
        var body = await CaptureAsync(
            new LlmRequest(
                model: "claude-opus-5",
                systemPrompt: null,
                messages: new[]
                {
                    LlmMessage.CreateAssistantMessage("I was answering something now archived."),
                    LlmMessage.CreateUserMessage("Carry on."),
                }
                )
            );

        var messages = body.RootElement.GetProperty("messages");

        Assert.Equal("user", messages[0].GetProperty("role").GetString());
        Assert.Equal(1, messages.GetArrayLength());
    }

    private static LlmRequest SimpleRequest()
    {
        return new LlmRequest(
            model: "claude-opus-5",
            systemPrompt: null,
            messages: new[] { LlmMessage.CreateUserMessage("hi") }
            );
    }

    private static async Task<CannedHttpHandler> SendAsync(
        LlmRequest request,
        Uri? endpoint = null
        )
    {
        var handler = CannedHttpHandler.ServerSentEvents(EmptyStream);
        var transport = new AnthropicMessagesTransport(
            endpoint ?? new Uri("https://api.anthropic.com"),
            "test-token",
            handler
            );

        await StreamEventCollector.CollectAsync(transport, request);

        return handler;
    }

    private static async Task<JsonDocument> CaptureAsync(
        LlmRequest request
        )
    {
        var handler = await SendAsync(request);

        return JsonDocument.Parse(handler.LastRequestBody);
    }
}
