using FreeAIr.Options2.Support;
using Microsoft.VisualStudio.Imaging.Interop;

namespace FreeAIr.UI.Embedillo.VisualLine.Command
{
    /// <summary>One autocomplete suggestion for a `/command` mention, pairing a support action with the icon and text shown in the picker.</summary>
    public sealed class CommandSuggestion : ISuggestion
    {
        /// <summary>Icon shown next to the suggestion in the autocomplete list.</summary>
        public ImageMoniker Image
        {
            get;
        }

        /// <summary>Full text used when resolving the mention back to its support action.</summary>
        public string FullData
        {
            get;
        }

        /// <summary>Text shown to the user and inserted into the prompt when the suggestion is picked.</summary>
        public string PublicData
        {
            get;
        }

        /// <summary>The support action this suggestion invokes, providing its prompt template.</summary>
        public SupportActionJson SupportAction
        {
            get;
        }

        /// <summary>Builds the suggestion from its display icon, data strings and backing support action.</summary>
        public CommandSuggestion(
            ImageMoniker image,
            string fullData,
            string publicData,
            SupportActionJson supportAction
            )
        {
            if (string.IsNullOrEmpty(fullData))
            {
                throw new ArgumentException($"'{nameof(fullData)}' cannot be null or empty.", nameof(fullData));
            }

            if (string.IsNullOrEmpty(publicData))
            {
                throw new ArgumentException($"'{nameof(publicData)}' cannot be null or empty.", nameof(publicData));
            }

            Image = image;
            FullData = fullData;
            PublicData = publicData;
            SupportAction = supportAction;
        }

    }
}
