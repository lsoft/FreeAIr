using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using OpenAI;
using OpenAI.Models;
using System.ClientModel;
using System.Linq;
using System.Threading.Tasks;

namespace FreeAIr.UI.ContextMenu
{
    public sealed class ModelContextMenu
    {
        public static async Task<string?> ChooseModelFromProviderAsync(
            string token,
            string endpoint,
            string title,
            string? preferredModelId = null
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

            if (!string.IsNullOrEmpty(preferredModelId))
            {
                var preferred = models.FirstOrDefault(a => a.Id == preferredModelId);
                if (preferred is not null)
                {
                    return preferred.Id;
                }
            }

            var chosen = await VisualStudioContextMenuCommandBridge.ShowAsync<OpenAIModel>(
                title,
                models
                    .ConvertAll(a => (a.Id, a as object))
                );
            if (chosen is null)
            {
                return null;
            }

            return chosen.Id;
        }

    }
}
