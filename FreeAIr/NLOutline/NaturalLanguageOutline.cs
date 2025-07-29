using System.Text.Json.Serialization;

namespace FreeAIr.NLOutline
{
    public sealed class NaturalLanguageOutline
    {
        [JsonPropertyName("file_path")]
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

        [JsonPropertyName("comment")]
        public string Comment
        {
            get;
            set;
        }

        public NaturalLanguageOutline()
        {
            FilePath = string.Empty;
            Line = 0;
            Comment = string.Empty;
        }
    }
}
