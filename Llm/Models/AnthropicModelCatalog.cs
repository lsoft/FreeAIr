using FreeAIr.Llm.Anthropic;
using FreeAIr.Llm.Wire;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Llm.Models
{
    /// <summary>
    /// The model list of an Anthropic endpoint (`GET /v1/models`). Hand written for the same reason
    /// the transport is: no Anthropic client targets .NET Framework 4.8.
    /// </summary>
    public sealed class AnthropicModelCatalog : ILlmModelCatalog
    {
        private readonly Uri _endpoint;
        private readonly string _token;
        private readonly TimeSpan _timeout;
        private readonly HttpMessageHandler? _handler;

        /// <summary>Binds a catalog to one endpoint; <paramref name="handler"/> is the tests' seam.</summary>
        public AnthropicModelCatalog(
            Uri endpoint,
            string? token,
            TimeSpan timeout,
            HttpMessageHandler? handler = null
            )
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _token = token ?? string.Empty;
            _timeout = timeout;
            _handler = handler;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<LlmModel>> GetModelsAsync(
            CancellationToken cancellationToken = default
            )
        {
            using var client = _handler is null
                ? new HttpClient()
                : new HttpClient(_handler, disposeHandler: false);
            client.Timeout = _timeout;

            using var request = new HttpRequestMessage(HttpMethod.Get, BuildModelsUri());
            request.Headers.Add("x-api-key", _token);
            request.Headers.Add("anthropic-version", AnthropicMessagesTransport.AnthropicVersion);

            using var response = await client.SendAsync(request, cancellationToken);

            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new LlmTransportException(
                    $"Service request failed. Status: {(int)response.StatusCode}",
                    ServerErrorMessageReader.Read(body),
                    (int)response.StatusCode
                    );
            }

            return ReadModels(body);
        }

        /// <summary>
        /// Reads `{"data":[{"id":"...","display_name":"..."}]}`. The display name stands in for the
        /// owner: it is the only other thing the reply says about a model, and it is what makes the
        /// picker's mask useful over a list of Claude versions.
        /// </summary>
        private static IReadOnlyList<LlmModel> ReadModels(
            string body
            )
        {
            var result = new List<LlmModel>();

            using var document = JsonDocument.Parse(body);

            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("data", out var data)
                || data.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var model in data.EnumerateArray())
            {
                if (model.ValueKind != JsonValueKind.Object
                    || !model.TryGetProperty("id", out var id)
                    || id.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var identifier = id.GetString();
                if (string.IsNullOrEmpty(identifier))
                {
                    continue;
                }

                string? displayName = null;
                if (model.TryGetProperty("display_name", out var display)
                    && display.ValueKind == JsonValueKind.String)
                {
                    displayName = display.GetString();
                }

                result.Add(new LlmModel(identifier!, displayName));
            }

            return result;
        }

        /// <summary>The models endpoint under the configured base, tolerating a base which already names the api version.</summary>
        private Uri BuildModelsUri()
        {
            var text = _endpoint.ToString().TrimEnd('/');

            if (text.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
            {
                return new Uri(text + "/models");
            }

            return new Uri(text + "/v1/models");
        }
    }
}
