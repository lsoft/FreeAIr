using Microsoft.VisualStudio.Imaging.Interop;

namespace FreeAIr.UI.Embedillo.VisualLine.SolutionItem
{
    /// <summary>One autocomplete suggestion for a `#file` mention: a solution file, project or solution node, with an optional text selection appended.</summary>
    public sealed class SolutionItemSuggestion : ISuggestion
    {
        /// <summary>Absolute path of the suggested solution item.</summary>
        private readonly string _fullPath;
        /// <summary>Path of the suggested item relative to the solution folder, shown to the user.</summary>
        private readonly string _relativePath;
        /// <summary>Text selection carried along with the mention, if the suggestion came from a selected code range.</summary>
        private readonly Answer.Parser.SelectedSpan _selection;

        /// <summary>Icon shown next to the suggestion in the autocomplete list.</summary>
        public ImageMoniker Image
        {
            get;
        }

        /// <summary>Full path plus selection, used when resolving the mention back to a file.</summary>
        public string FullData
        {
            get;
        }

        /// <summary>Relative path plus selection, shown to the user and inserted into the prompt text.</summary>
        public string PublicData
        {
            get;
        }

        /// <summary>Builds the suggestion from a solution item's full and relative paths and its optional selection.</summary>
        public SolutionItemSuggestion(
            ImageMoniker image,
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

            Image = image;
            _fullPath = fullPath;
            _relativePath = relativePath;
            _selection = selection;

            FullData = fullPath + selection?.ToString();
            PublicData = relativePath + selection?.ToString();
        }

    }
}
