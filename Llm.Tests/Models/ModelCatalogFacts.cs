using FreeAIr.Llm.Models;
using FreeAIr.Llm.Tests.Fakes;
using System.ClientModel.Primitives;
using System.Net;
using Xunit;

namespace FreeAIr.Llm.Tests.Models;

/// <summary>
/// How the model picker and the wizard's reachability check ask an endpoint what it serves. The two
/// protocols disagree about the route, the authentication header and the shape of the answer, so
/// the question is asked by a catalog of its own rather than by one client with a flag.
/// </summary>
public sealed class ModelCatalogFacts
{
    [Fact]
    public async Task AnOpenAiEndpointIsAskedForItsModelList()
    {
        var handler = CannedHttpHandler.Json(
            """{"object":"list","data":[{"id":"gpt-4o","object":"model","owned_by":"openai"},{"id":"local-model","object":"model","owned_by":"lmstudio"}]}"""
            );

        var catalog = new OpenAiModelCatalog(
            new Uri("http://localhost:1234/v1"),
            "test-token",
            TimeSpan.FromSeconds(15),
            new HttpClientPipelineTransport(new HttpClient(handler))
            );

        var models = await catalog.GetModelsAsync();

        Assert.Equal(new[] { "gpt-4o", "local-model" }, models.Select(m => m.Id).ToArray());
        Assert.Equal("openai", models[0].OwnedBy);
    }

    [Fact]
    public async Task AnAnthropicEndpointIsAskedWithItsOwnHeaders()
    {
        var handler = CannedHttpHandler.Json(
            """{"data":[{"type":"model","id":"claude-opus-5","display_name":"Claude Opus 5"}],"has_more":false}"""
            );

        var catalog = new AnthropicModelCatalog(
            new Uri("https://api.anthropic.com"),
            "test-token",
            TimeSpan.FromSeconds(15),
            handler
            );

        var models = await catalog.GetModelsAsync();

        Assert.Equal(
            "https://api.anthropic.com/v1/models",
            handler.LastRequest!.RequestUri!.ToString()
            );
        Assert.Equal("test-token", Assert.Single(handler.LastRequest.Headers.GetValues("x-api-key")));

        var model = Assert.Single(models);
        Assert.Equal("claude-opus-5", model.Id);
        //the display name stands in for the owner - it is the only other thing the reply says
        Assert.Equal("Claude Opus 5", model.OwnedBy);
    }

    [Fact]
    public async Task AnUnreachableAnthropicEndpointReportsWhatTheServerSaid()
    {
        //the wizard shows this sentence in its status line, which is the whole point of asking
        var handler = CannedHttpHandler.Failure(
            HttpStatusCode.Unauthorized,
            """{"type":"error","error":{"type":"authentication_error","message":"invalid x-api-key"}}"""
            );

        var catalog = new AnthropicModelCatalog(
            new Uri("https://api.anthropic.com"),
            "wrong-token",
            TimeSpan.FromSeconds(15),
            handler
            );

        var excp = await Assert.ThrowsAsync<LlmTransportException>(() => catalog.GetModelsAsync());

        Assert.Equal("invalid x-api-key", excp.ServerMessage);
        Assert.Equal(401, excp.StatusCode);
    }

    [Fact]
    public void TheFactoryPicksTheCatalogTheProtocolCallsFor()
    {
        Assert.IsType<OpenAiModelCatalog>(
            LlmModelCatalogFactory.Create(LlmProtocol.OpenAi, new Uri("http://localhost:1234/v1"), null)
            );
        Assert.IsType<AnthropicModelCatalog>(
            LlmModelCatalogFactory.Create(LlmProtocol.Anthropic, new Uri("https://api.anthropic.com"), null)
            );
    }
}
