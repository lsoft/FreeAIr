using FreeAIr.Llm.OpenAi;
using FreeAIr.Llm.Tests.Fakes;
using System.ClientModel.Primitives;
using System.Text.Json;
using Xunit;

namespace FreeAIr.Llm.Tests.OpenAi;

/// <summary>
/// What FreeAIr actually puts on the wire in the OpenAI chat completions protocol.
///
/// These are characterization tests: they pin the request down as it is today, before the second
/// protocol is added, so that the refactoring which introduces it cannot change the dialect the
/// endpoints already in use are spoken to in.
/// </summary>
public sealed class OpenAiRequestFacts
{
    /// <summary>Enough of a stream to let a request finish; the tests here look at what was sent, not at what came back.</summary>
    private const string EmptyStream = "data: [DONE]\n\n";

    [Fact]
    public async Task SystemPromptTravelsAsSystemRole()
    {
        //this is the regression the neutral model was introduced to prevent: appending the system
        //prompt to a message list as a bare string goes through ChatMessage's implicit conversion,
        //which builds a *user* message, and the agent's instructions stop being instructions
        var body = await CaptureAsync(
            new LlmRequest(
                model: "gpt-4o",
                systemPrompt: "You are a helpful assistant.",
                messages: new[] { LlmMessage.CreateUserMessage("hi") }
                )
            );

        var messages = body.RootElement.GetProperty("messages");

        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
    }

    [Fact]
    public async Task NoSystemMessageIsSentWhenTheAgentHasNoPrompt()
    {
        var body = await CaptureAsync(
            new LlmRequest(
                model: "gpt-4o",
                systemPrompt: null,
                messages: new[] { LlmMessage.CreateUserMessage("hi") }
                )
            );

        var messages = body.RootElement.GetProperty("messages");

        Assert.Equal(1, messages.GetArrayLength());
        Assert.Equal("user", messages[0].GetProperty("role").GetString());
    }

    [Fact]
    public async Task AToolCallAndItsResultBecomeAnAssistantMessageAndAToolMessage()
    {
        var body = await CaptureAsync(
            new LlmRequest(
                model: "gpt-4o",
                systemPrompt: null,
                messages: new[]
                {
                    LlmMessage.CreateUserMessage("build it"),
                    LlmMessage.CreateAssistantToolCallMessage(
                        new[] { new LlmToolCall("call_1", "VS.Build", """{"configuration":"Debug"}""") }
                        ),
                    LlmMessage.CreateToolResultMessage(
                        new LlmToolResult("call_1", "Build succeeded.")
                        ),
                }
                )
            );

        var messages = body.RootElement.GetProperty("messages");

        var assistant = messages[1];
        Assert.Equal("assistant", assistant.GetProperty("role").GetString());
        var toolCall = assistant.GetProperty("tool_calls")[0];
        Assert.Equal("call_1", toolCall.GetProperty("id").GetString());
        Assert.Equal("VS.Build", toolCall.GetProperty("function").GetProperty("name").GetString());
        Assert.Contains(
            "Debug",
            toolCall.GetProperty("function").GetProperty("arguments").GetString()
            );

        var tool = messages[2];
        Assert.Equal("tool", tool.GetProperty("role").GetString());
        Assert.Equal("call_1", tool.GetProperty("tool_call_id").GetString());
    }

    [Fact]
    public async Task AnOfferedToolCarriesItsSchemaAndIsDeclaredStrict()
    {
        var body = await CaptureAsync(
            new LlmRequest(
                model: "gpt-4o",
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

        var function = body.RootElement.GetProperty("tools")[0].GetProperty("function");

        Assert.Equal("VS.SearchFileContent", function.GetProperty("name").GetString());
        Assert.Equal("Searches the solution.", function.GetProperty("description").GetString());
        Assert.True(function.GetProperty("strict").GetBoolean());
        Assert.True(
            function.GetProperty("parameters").GetProperty("properties").TryGetProperty("query", out _)
            );
        Assert.Equal("auto", body.RootElement.GetProperty("tool_choice").GetString());
    }

    [Fact]
    public async Task AToolDeclaringNoParametersStillGetsAnObjectSchema()
    {
        //LM Studio validates function.parameters and answers the *whole* request with 400 when a
        //single offered tool declares a bare {}, which takes the chat down with it
        var body = await CaptureAsync(
            new LlmRequest(
                model: "gpt-4o",
                systemPrompt: null,
                messages: new[] { LlmMessage.CreateUserMessage("hi") },
                tools: new[] { new LlmToolDefinition("VS.GetSolutionTree", "Reads the tree.", "{}") }
                )
            );

        var parameters = body.RootElement
            .GetProperty("tools")[0]
            .GetProperty("function")
            .GetProperty("parameters")
            ;

        Assert.Equal("object", parameters.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Object, parameters.GetProperty("properties").ValueKind);
    }

    [Fact]
    public async Task NoToolsMeansToolChoiceNone()
    {
        //some providers reject a request which asks for `auto` over an empty tool list
        var body = await CaptureAsync(
            new LlmRequest(
                model: "gpt-4o",
                systemPrompt: null,
                messages: new[] { LlmMessage.CreateUserMessage("hi") },
                tools: Array.Empty<LlmToolDefinition>(),
                toolChoice: LlmToolChoice.Auto
                )
            );

        Assert.Equal("none", body.RootElement.GetProperty("tool_choice").GetString());
        Assert.False(body.RootElement.TryGetProperty("tools", out var tools) && tools.GetArrayLength() > 0);
    }

    [Fact]
    public async Task TheOutputTokenLimitIsSentWhenTheSettingsNameOne()
    {
        var body = await CaptureAsync(
            new LlmRequest(
                model: "gpt-4o",
                systemPrompt: null,
                messages: new[] { LlmMessage.CreateUserMessage("hi") },
                maxOutputTokens: 4096
                )
            );

        Assert.Equal(4096, body.RootElement.GetProperty("max_completion_tokens").GetInt32());
    }

    /// <summary>Runs one request against a handler which answers with an empty stream, and returns the JSON that was sent.</summary>
    private static async Task<JsonDocument> CaptureAsync(
        LlmRequest request
        )
    {
        var handler = CannedHttpHandler.ServerSentEvents(EmptyStream);
        var transport = new OpenAiChatTransport(
            new Uri("http://localhost:1234/v1"),
            "test-token",
            new HttpClientPipelineTransport(new HttpClient(handler))
            );

        await StreamEventCollector.CollectAsync(transport, request);

        return JsonDocument.Parse(handler.LastRequestBody);
    }
}
