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
        /// <summary>
        /// The text used to match a model's id (or owner) — either a plain substring or a regex
        /// pattern, depending on <see cref="IsRegex"/>.
        /// </summary>
        public string ModelNameMask
        {
            get;
        }
        /// <summary>
        /// Whether <see cref="ModelNameMask"/> should be interpreted as a regular expression
        /// rather than a plain case-insensitive substring.
        /// </summary>
        public bool IsRegex
        {
            get;
        }

        /// <summary>
        /// Creates a filter for the model picker's list using the given name mask and matching mode.
        /// </summary>
        public ModelFilterer(
            string modelNameMask,
            bool isRegex
            )
        {
            ModelNameMask = modelNameMask;
            IsRegex = isRegex;
        }

        /// <summary>
        /// Narrows a provider's model collection down to the ones matching <see cref="ModelNameMask"/>,
        /// falling back to a plain substring match if the mask is not valid as a regex.
        /// </summary>
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

        /// <summary>
        /// Builds the predicate used to test a model's id and owner against the mask: match-all when
        /// the mask is empty, a regex match when requested, otherwise a case-insensitive substring match.
        /// </summary>
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
