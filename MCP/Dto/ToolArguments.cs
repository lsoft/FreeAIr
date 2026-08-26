using System.Collections.Generic;
using System.Text.Json;

namespace Dto
{
    /// <summary>
    /// Converts the arguments of a tool call between the objects the VSIX builds out of the model's
    /// answer and the raw JSON text <see cref="CallToolRequest.ArgumentsJson"/> carries to the proxy
    /// process. The tool schema travels the other way as raw JSON text too, see
    /// <see cref="GetToolReply.Parameters"/>.
    ///
    /// Text is the whole point. The channel to the proxy is StreamJsonRpc with its default
    /// Newtonsoft formatter, and a weakly typed member on the wire comes back as a JObject or a
    /// JArray for everything that is not a primitive. System.Text.Json - the serializer the MCP SDK
    /// builds `tools/call` with - has no idea what those are, sees that they implement
    /// IEnumerable&lt;JToken&gt; and writes out the token's *children* instead of its value, so every
    /// nested object and array in the arguments used to reach the MCP server as nested empty arrays.
    /// A string has no runtime type left to lose.
    /// </summary>
    public static class ToolArguments
    {
        /// <summary>
        /// Renders a tool call's arguments as the JSON object the MCP specification asks for, or
        /// null when there are none. Values may be arbitrarily nested; anything
        /// <see cref="JsonSerializer"/> can write is carried through unchanged.
        /// </summary>
        public static string? Serialize(
            IReadOnlyDictionary<string, object?>? arguments
            )
        {
            if (arguments is null)
            {
                return null;
            }

            return JsonSerializer.Serialize(arguments);
        }

        /// <summary>
        /// Parses what <see cref="Serialize"/> produced back into the per-argument JSON values the
        /// MCP SDK expects. The elements are self-contained, so the caller may outlive this call.
        /// </summary>
        public static IReadOnlyDictionary<string, JsonElement>? Deserialize(
            string? argumentsJson
            )
        {
            if (string.IsNullOrWhiteSpace(argumentsJson))
            {
                return null;
            }

            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(argumentsJson!);
        }
    }
}
