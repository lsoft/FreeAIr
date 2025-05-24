using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Agent
{
    //public class FunctionParameter
    //{
    //    public string ParameterName
    //    {
    //        get;
    //    }
    //    public string Description
    //    {
    //        get;
    //    }
    //    public string Type
    //    {
    //        get;
    //    }

    //    public FunctionParameter(
    //        JsonNode node
    //        )
    //    {
    //        ParameterName = node.GetPropertyName();
    //        var temp = JsonSerializer.Deserialize<HelperObject>(node);
    //        Description = temp.Description;
    //        Type = temp.Type;
    //    }

    //    private class HelperObject
    //    {
    //        [JsonPropertyName("description")]
    //        public string Description
    //        {
    //            get;
    //            set;
    //        }

    //        [JsonPropertyName("type")]
    //        public string Type
    //        {
    //            get;
    //            set;
    //        }
    //    }

    //}
}
