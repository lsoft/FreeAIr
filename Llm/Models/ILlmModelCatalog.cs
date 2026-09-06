using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Llm.Models
{
    /// <summary>
    /// Asks an endpoint which models it serves. One implementation per protocol, because the two
    /// disagree about both the route and the reply: OpenAI answers `GET /models` with
    /// `{data:[{id, owned_by}]}` behind a bearer token, Anthropic answers `GET /v1/models` with
    /// `{data:[{id, display_name}]}` behind `x-api-key`.
    ///
    /// It is the model picker and the wizard's `test endpoint` button that need this, so a failure
    /// here is a message in a status line rather than something to recover from.
    /// </summary>
    public interface ILlmModelCatalog
    {
        /// <summary>The models the endpoint offers, in the order it lists them.</summary>
        Task<IReadOnlyList<LlmModel>> GetModelsAsync(
            CancellationToken cancellationToken = default
            );
    }
}
