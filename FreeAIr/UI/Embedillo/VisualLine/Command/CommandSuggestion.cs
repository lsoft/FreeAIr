using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.UI.Embedillo.VisualLine.Command
{
    public sealed class CommandSuggestion : ISuggestion
    {
        public string FullData
        {
            get;
        }
        
        public string PublicData
        {
            get;
        }

        public CommandSuggestion(
            string fullData,
            string publicData
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

            FullData = fullData;
            PublicData = publicData;
        }

    }
}
