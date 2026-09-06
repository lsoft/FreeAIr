using OpenAI;
using OpenAI.Models;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Llm.Models
{
    /// <summary>
    /// The model list of an OpenAI compatible endpoint, read through the SDK's own model client.
    /// </summary>
    public sealed class OpenAiModelCatalog : ILlmModelCatalog
    {
        private readonly Uri _endpoint;
        private readonly string _token;
        private readonly TimeSpan _timeout;
        private readonly PipelineTransport? _pipelineTransport;

        /// <summary>
        /// Binds a catalog to one endpoint. The timeout is short by the standards of this product -
        /// nobody watches a model picker for an hour - and the caller chooses it, because the
        /// wizard's reachability check wants to give up sooner than the picker does.
        /// </summary>
        public OpenAiModelCatalog(
            Uri endpoint,
            string? token,
            TimeSpan timeout,
            PipelineTransport? pipelineTransport = null
            )
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _token = token ?? string.Empty;
            _timeout = timeout;
            _pipelineTransport = pipelineTransport;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<LlmModel>> GetModelsAsync(
            CancellationToken cancellationToken = default
            )
        {
            var options = new OpenAIClientOptions
            {
                NetworkTimeout = _timeout,
                Endpoint = _endpoint,
            };

            if (_pipelineTransport is not null)
            {
                options.Transport = _pipelineTransport;
            }

            var client = new OpenAIModelClient(
                new ApiKeyCredential(_token),
                options
                );

            var models = (await client.GetModelsAsync(cancellationToken)).Value;

            var result = new List<LlmModel>(models.Count);
            foreach (var model in models)
            {
                result.Add(new LlmModel(model.Id, model.OwnedBy));
            }

            return result;
        }
    }
}
