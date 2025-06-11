using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FreeAIr.NLOutline
{
    public sealed class NaturalLanguageOutline
    {
        [JsonPropertyName("filepath")]
        public string FilePath
        {
            get;
            set;
        }

        [JsonPropertyName("line")]
        public int Line
        {
            get;
            set;
        }

        [JsonPropertyName("text")]
        public string Text
        {
            get;
            set;
        }

        public NaturalLanguageOutline()
        {
            FilePath = string.Empty;
            Line = 0;
            Text = string.Empty;
        }
    }
}
