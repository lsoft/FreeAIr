using System.Text.Json;
using System.Text.Json.Serialization;

namespace FreeAIr.Options2.Support
{
    /// <summary>
    /// Reads and writes an enum as its member name rather than a number, so a settings file names
    /// values like <see cref="SupportScopeEnum"/> the way the user would type them instead of an
    /// opaque integer.
    /// </summary>
    public sealed class StringEnumConverter<T> : JsonConverter<T>
        where T : struct, Enum
    {
        /// <summary>Parses the enum member name, case-insensitively, throwing when it does not match any value.</summary>
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var enumString = reader.GetString();
            if (Enum.TryParse(enumString, true, out T result))
            {
                return result;
            }
            throw new JsonException($"Не удалось преобразовать '{enumString}' в тип {typeof(T)}.");
        }

        /// <summary>Writes the enum's member name as a json string.</summary>
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
