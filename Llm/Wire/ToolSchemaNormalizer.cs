using System.IO;
using System.Text;
using System.Text.Json;

namespace FreeAIr.Llm.Wire
{
    /// <summary>
    /// Brings a tool's argument schema to the shape every endpoint accepts before it is written
    /// into a request.
    ///
    /// This is not cosmetic. A tool without arguments is habitually declared as `{}` - both by
    /// FreeAIr's own Visual Studio tools and by third party MCP servers, whose schemas arrive here
    /// unseen - and LM Studio rejects the entire completion request with 400 Bad Request when one
    /// such tool is offered, taking the whole chat down with it. Anthropic is no more forgiving: an
    /// `input_schema` which is not an object schema fails validation for the request as a whole.
    ///
    /// It lives with the transports rather than with the MCP code because it is a rule of the wire,
    /// not of MCP, and both protocols need it applied identically.
    /// </summary>
    public static class ToolSchemaNormalizer
    {
        /// <summary>
        /// The schema a tool taking no arguments has to declare. A bare `{}` is not a valid
        /// function schema.
        /// </summary>
        public const string NoParameters =
            """
            {
                "type": "object",
                "properties": {}
            }
            """;

        /// <summary>
        /// Returns an object schema carrying a `properties` member, repairing the given one rather
        /// than replacing it, so that a schema which merely forgot its `type` keeps the parameters
        /// it does declare. A schema which is not even JSON cannot be repaired and is replaced by
        /// <see cref="NoParameters"/>.
        /// </summary>
        public static string Normalize(
            string? parameters
            )
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                return NoParameters;
            }

            try
            {
                using var document = JsonDocument.Parse(parameters!);

                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                {
                    return NoParameters;
                }

                var hasObjectType =
                    root.TryGetProperty("type", out var type)
                    && type.ValueKind == JsonValueKind.String
                    && type.ValueEquals("object")
                    ;
                var hasProperties =
                    root.TryGetProperty("properties", out var properties)
                    && properties.ValueKind == JsonValueKind.Object
                    ;
                if (hasObjectType && hasProperties)
                {
                    return parameters!;
                }

                using var stream = new MemoryStream();
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();

                    //the two members the endpoints insist on come first, then everything the
                    //original schema said which we have not just written ourselves
                    writer.WriteString("type", "object");
                    if (!hasProperties)
                    {
                        writer.WriteStartObject("properties");
                        writer.WriteEndObject();
                    }

                    foreach (var member in root.EnumerateObject())
                    {
                        if (member.NameEquals("type"))
                        {
                            continue;
                        }
                        if (member.NameEquals("properties") && !hasProperties)
                        {
                            continue;
                        }

                        member.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                }

                return Encoding.UTF8.GetString(stream.ToArray());
            }
            catch (JsonException)
            {
                //a schema which is not even json cannot be repaired, only replaced
            }

            return NoParameters;
        }
    }
}
