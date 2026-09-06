using FreeAIr.Helper;
using FreeAIr.Llm;
using FreeAIr.Llm.Models;
using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FreeAIr.UI.ContextMenu
{
    /// <summary>
    /// Queries an endpoint for its available models and shows a Visual Studio context menu so the
    /// user can pick one, optionally narrowed by a <see cref="ModelFilterer"/>. Which route the
    /// question takes is decided by the agent's protocol, through
    /// <see cref="LlmModelCatalogFactory"/>.
    /// </summary>
    public static class ModelContextMenu
    {
        /// <summary>
        /// Fetches the model list from the given endpoint/token, filters it if a
        /// <see cref="ModelFilterer"/> is supplied, and shows the picker menu, returning the
        /// chosen model id (or the only one, if just a single model matches).
        /// </summary>
        public static async Task<string?> ChooseModelFromProviderAsync(
            string token,
            string endpoint,
            LlmProtocol protocol,
            string title,
            ModelFilterer? filterer = null
            )
        {
            if (title is null)
            {
                throw new ArgumentNullException(nameof(title));
            }

            var uri = UriHelper.TryBuildEndpointUri(endpoint);
            if (uri is null)
            {
                return null;
            }

            var catalog = LlmModelCatalogFactory.Create(
                protocol,
                uri,
                token,
                TimeSpan.FromHours(1)
                );
            var models = await catalog.GetModelsAsync();

            if (models.Count == 1)
            {
                return models[0].Id;
            }

            var filteredModels = filterer is not null
                ? filterer.Apply(models)
                : new List<LlmModel>(models)
                ;
            if (filteredModels is null
                || filteredModels.Count == 0)
            {
                return null;
            }

            if (models.Count > 30)
            {
                title += $" ({filteredModels.Count} out of {models.Count})";
            }

            var chosen = await VisualStudioContextMenuCommandBridge.ShowAsync<LlmModel>(
                title,
                filteredModels.ConvertAll(m => (m.Id, (object)m))
                );

            if (chosen is null)
            {
                return null;
            }

            return chosen.Id;
        }


    }
}
