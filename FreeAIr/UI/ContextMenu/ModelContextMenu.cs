using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using Microsoft.Build.Utilities;
using OpenAI;
using OpenAI.Models;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FreeAIr.UI.ContextMenu
{
    /// <summary>
    /// Queries an OpenAI compatible endpoint for its available models and shows a Visual Studio
    /// context menu so the user can pick one, optionally narrowed by a <see cref="ModelFilterer"/>.
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

            var modelClient = new OpenAIModelClient(
                new ApiKeyCredential(
                    token
                    ),
                new OpenAIClientOptions
                {
                    NetworkTimeout = TimeSpan.FromHours(1),
                    Endpoint = uri,
                }
                );
            var models = (await modelClient.GetModelsAsync()).Value;

            if (models.Count == 1)
            {
                return models[0].Id;
            }

            var filteredModels = filterer is not null
                ? filterer.Apply(models)
                : new List<OpenAIModel>(models)
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

            var chosen = await VisualStudioContextMenuCommandBridge.ShowAsync<OpenAIModel>(
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
