using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FreeAIr.NLOutline
{
    /// <summary>
    /// The wrapper the LLM's natural language outline response is deserialized into: a list of
    /// <see cref="NaturalLanguageOutline"/> comments covering one or more source files.
    /// </summary>
    public sealed class NaturalLanguageOutlineCollection
    {
        /// <summary>
        /// The outline comments returned by the LLM, one per documented location in the scanned
        /// source.
        /// </summary>
        [JsonPropertyName("comments")]
        public List<NaturalLanguageOutline> Comments
        {
            get;
            set;
        }

        /// <summary>Creates an empty collection ready to receive deserialized comments.</summary>
        public NaturalLanguageOutlineCollection()
        {
            Comments = new();
        }

        /// <summary>
        /// Parses the LLM's JSON reply into a <see cref="NaturalLanguageOutlineCollection"/>,
        /// tolerating both the expected wrapper shape and a bare array of comments in case the
        /// model omits the wrapping object.
        /// </summary>
        public static bool TryParse(string json, out NaturalLanguageOutlineCollection? result)
        {
            try
            {
                var tresult = new NaturalLanguageOutlineCollection
                {
                };

                try
                {
                    result = System.Text.Json.JsonSerializer.Deserialize<NaturalLanguageOutlineCollection>(json);
                    return true;
                }
                catch
                {
                    //nothing to do
                }

                try
                {
                    tresult.Comments = System.Text.Json.JsonSerializer.Deserialize<List<NaturalLanguageOutline>>(json);
                    result = tresult;
                    return true;
                }
                catch
                {
                    //nothing to do
                }

            }
            catch
            {
                //nothing to do
            }

            //todo log of json string with message 'cannot deserialize'

            result = null;
            return false;
        }
    }
}
