using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.UI.Embedillo.VisualLine.SolutionItem
{
    public sealed class SolutionItemSuggestion : ISuggestion
    {
        private readonly string _fullPath;
        private readonly string _relativePath;
        private readonly Answer.Parser.SelectedSpan _selection;

        public string FullData
        {
            get;
        }

        public string PublicData
        {
            get;
        }

        public SolutionItemSuggestion(
            string fullPath,
            string relativePath,
            Answer.Parser.SelectedSpan? selection
            )
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                throw new ArgumentException($"'{nameof(fullPath)}' cannot be null or empty.", nameof(fullPath));
            }

            if (string.IsNullOrEmpty(relativePath))
            {
                throw new ArgumentException($"'{nameof(relativePath)}' cannot be null or empty.", nameof(relativePath));
            }

            _fullPath = fullPath;
            _relativePath = relativePath;
            _selection = selection;

            FullData = fullPath + selection?.ToString();
            PublicData = relativePath + selection?.ToString();
        }

    }
}
