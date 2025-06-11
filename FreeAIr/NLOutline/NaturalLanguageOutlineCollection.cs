using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FreeAIr.NLOutline
{
    public sealed class NaturalLanguageOutlineCollection
    {
        [JsonPropertyName("comments")]
        public List<NaturalLanguageOutline> Comments
        {
            get;
            set;
        }

        public NaturalLanguageOutlineCollection()
        {
            Comments = new();
        }

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
