using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using OpenAI;
using OpenAI.Models;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FreeAIr.UI.ContextMenu
{
    public static class ModelContextMenu
    {
        public static async Task<string?> ChooseModelFromProviderAsync(
            string token,
            string endpoint,
            string title,
            string? preferredModelId = null,
            string mask = null, bool isRegex = false
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

            List<(string, object)> _filtredModels = null;
            var filter = _getFilter(mask, isRegex);
            try
            {
                _filtredModels = models.Where(m => filter(m)).Select(m => (m.Id, (object)m)).ToList();
            }
            catch (Exception ex) when (ex is ArgumentException
                                    or ArgumentNullException
                                    or ArgumentOutOfRangeException
                                    or RegexMatchTimeoutException)
            {
                filter = _getFilter(mask, false);
                _filtredModels = models.Where(m => filter(m)).Select(m => (m.Id, (object)m)).ToList();
            }
            if (models.Count > 30) title += $" ({_filtredModels.Count} out of {models.Count})";

            var chosen = await VisualStudioContextMenuCommandBridge.ShowAsync<OpenAIModel>(title,_filtredModels);

            if (chosen is null)
            {
                return null;
            }

            return chosen.Id;
        }

        static Func<OpenAIModel, bool> _getFilter(string mask, bool isRegex)
        {
            Func<OpenAIModel, bool> filter;
            if (string.IsNullOrEmpty(mask))
            {
                filter = _ => true;
            }
            else if (isRegex)
            {
                var regex = new Regex(mask, RegexOptions.IgnoreCase);
                filter = m => regex.IsMatch(m.Id) || (m.OwnedBy != null && regex.IsMatch(m.OwnedBy));
            }
            else
            {
                filter = m => m.Id.Contains(mask, StringComparison.OrdinalIgnoreCase)
                           || (m.OwnedBy?.Contains(mask, StringComparison.OrdinalIgnoreCase) ?? false);
            }
            return (filter);
        }

    }
}
