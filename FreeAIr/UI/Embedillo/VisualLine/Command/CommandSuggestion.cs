using FreeAIr.Options2.Support;
using Microsoft.VisualStudio.Imaging.Interop;

namespace FreeAIr.UI.Embedillo.VisualLine.Command
{
    public sealed class CommandSuggestion : ISuggestion
    {
        public ImageMoniker Image
        {
            get;
        }

        public string FullData
        {
            get;
        }
        
        public string PublicData
        {
            get;
        }

        public SupportActionJson SupportAction
        {
            get;
        }

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
