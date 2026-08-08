using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace FreeAIr.Search.Tests
{
    /// <summary>
    /// Where the integration tests find a real embedding server.
    ///
    /// Nothing is hard coded: a machine without a server runs the unit tests and skips these, which
    /// is why the address comes from the environment rather than from a file in the repository.
    /// </summary>
    internal static class IntegrationEnvironment
    {
        /// <summary>The OpenAI compatible base address, e.g. <c>http://localhost:5001/v1</c>.</summary>
        public const string EndpointVariableName = "FREEAIR_TEST_EMBEDDING_ENDPOINT";

        /// <summary>
        /// The model to ask for. Optional: when it is not given, the first model the server lists
        /// is used, which is what a local one-model server wants.
        /// </summary>
        public const string ModelVariableName = "FREEAIR_TEST_EMBEDDING_MODEL";

        /// <summary>Optional; local servers usually want no key at all.</summary>
        public const string TokenVariableName = "FREEAIR_TEST_EMBEDDING_TOKEN";

        public static string? Endpoint => NormalizeEndpoint(Read(EndpointVariableName));

        public static string? Model => Read(ModelVariableName);

        public static string? Token => Read(TokenVariableName);

        /// <summary>Null when the tests can run; the reason xunit will print otherwise.</summary>
        public static string? SkipReason =>
            Endpoint is null
                ? $"no embedding server: set {EndpointVariableName} (for example http://localhost:5001/v1) to run this test"
                : null;

        public static string RequiredEndpoint =>
            Endpoint ?? throw new InvalidOperationException($"{EndpointVariableName} is not set.");

        /// <summary>
        /// The models the server admits to having. This is the first thing an integration test
        /// should ask: a wrong port answers nothing, a running server with no model loaded answers
        /// an empty list, and both are worth telling apart from a bad vector.
        /// </summary>
        public static async Task<IReadOnlyList<string>> ListModelsAsync(
            CancellationToken cancellationToken
            )
        {
            using var client = CreateClient();

            using var response = await client.GetAsync(
                RequiredEndpoint + "/models",
                cancellationToken
                );
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            var result = new List<string>();
            if (json.RootElement.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Array)
            {
                foreach (var model in data.EnumerateArray())
                {
                    if (model.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                    {
                        result.Add(id.GetString()!);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// The model name to send. The one from the environment wins; otherwise the server is asked
        /// what it has.
        /// </summary>
        public static async Task<string> ResolveModelAsync(
            CancellationToken cancellationToken
            )
        {
            var configured = Model;
            if (configured is not null)
            {
                return configured;
            }

            var models = await ListModelsAsync(cancellationToken);

            Assert.True(
                models.Count > 0,
                $"{RequiredEndpoint} answered, but lists no model at all. Load one, or name it in {ModelVariableName}."
                );

            return models[0];
        }

        private static HttpClient CreateClient()
        {
            var result = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30),
            };

            var token = Token;
            if (!string.IsNullOrWhiteSpace(token))
            {
                result.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return result;
        }

        private static string? Read(
            string name
            )
        {
            var result = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
        }

        private static string? NormalizeEndpoint(
            string? endpoint
            )
        {
            //"http://localhost:5001/v1/" and "http://localhost:5001/v1" are the same address to a
            //human and two different ones to a Uri which is about to have a path appended to it
            return endpoint?.TrimEnd('/');
        }
    }

    /// <summary>
    /// A <see cref="FactAttribute"/> which skips itself when there is no server to talk to, so that
    /// a plain `dotnet test` stays green on a machine which has none.
    /// </summary>
    public sealed class IntegrationFactAttribute : FactAttribute
    {
        public IntegrationFactAttribute()
        {
            Skip = IntegrationEnvironment.SkipReason;
        }
    }
}
