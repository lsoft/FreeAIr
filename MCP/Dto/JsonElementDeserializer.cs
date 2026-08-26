using System.Collections.Generic;
using System.Text.Json;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Turns a parsed JSON value into plain dictionaries, lists and primitives. It is how the
    /// arguments of a model's tool call stop being a <see cref="JsonDocument"/> the caller has to
    /// keep alive, so a Visual Studio tool can read them by name and
    /// <see cref="Dto.ToolArguments"/> can write them back out for an external MCP server.
    /// </summary>
    public static class JsonElementDeserializer
    {
        /// <summary>Rebuilds <paramref name="element"/> as a Dictionary/List/primitive tree, recursing through objects and arrays.</summary>
        public static object? DeserializeToObject(
            this JsonElement element
            )
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return null;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    if (element.TryGetDouble(out double doubleValue))
                        return doubleValue;
                    return element.GetDecimal();
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        dict[prop.Name] = DeserializeToObject(prop.Value);
                    }
                    return dict;
                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                    {
                        list.Add(DeserializeToObject(item));
                    }
                    return list;
                default:
                    throw new InvalidOperationException($"Cannot deserialize JsonElement: {element.GetRawText()}");
            }
        }
    }
}
