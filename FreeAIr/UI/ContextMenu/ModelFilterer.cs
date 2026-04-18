using OpenAI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FreeAIr.UI.ContextMenu
{
    /// <summary>
    /// Фильтр для списка моделей
    /// </summary>
    public sealed class ModelFilterer
    {
        public string ModelNameMask
        {
            get;
        }
        public bool IsRegex
        {
            get;
        }

        public ModelFilterer(
            string modelNameMask,
            bool isRegex
            )
        {
            ModelNameMask = modelNameMask;
            IsRegex = isRegex;
        }

        public List<OpenAIModel>? Apply(
            OpenAIModelCollection models
            )
        {
            List<OpenAIModel> filteredModels = null;
            
            var filter = ComposeFilter(ModelNameMask, IsRegex);
            try
            {
                filteredModels = models
                    .Where(m => filter(m))
                    .ToList()
                    ;
            }
            catch (Exception ex)
            when (ex is ArgumentException
                  or ArgumentNullException
                  or ArgumentOutOfRangeException
                  or RegexMatchTimeoutException)
            {
                filter = ComposeFilter(ModelNameMask, false);
                filteredModels = models
                    .Where(m => filter(m))
                    .ToList()
                    ;
            }

            return filteredModels;
        }

        private static Func<OpenAIModel, bool> ComposeFilter(
            string modelNameMask,
            bool isRegex
            )
        {
            Func<OpenAIModel, bool> filter;
            if (string.IsNullOrEmpty(modelNameMask))
            {
                filter = _ => true;
            }
            else if (isRegex)
            {
                var regex = new Regex(modelNameMask, RegexOptions.IgnoreCase);
                filter = m => regex.IsMatch(m.Id) || (m.OwnedBy != null && regex.IsMatch(m.OwnedBy));
            }
            else
            {
                filter = m => m.Id.Contains(modelNameMask, StringComparison.OrdinalIgnoreCase)
                           || (m.OwnedBy?.Contains(modelNameMask, StringComparison.OrdinalIgnoreCase) ?? false);
            }
            return filter;
        }

    }
}
