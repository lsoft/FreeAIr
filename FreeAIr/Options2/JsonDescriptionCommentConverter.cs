using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FreeAIr.Options2
{
    /// <summary>
    /// A json converter that writes each property's <see cref="DescriptionAttribute"/> as a `/* ... */`
    /// comment right above it, so a hand edited settings file explains its own fields in place. Reading
    /// tolerates the comments (and any property order) since the base <see cref="JsonSerializerOptions"/>
    /// is set to skip them.
    /// </summary>
    public class JsonDescriptionCommentConverter<T> : JsonConverter<T>
        where T : class, new()
    {
        /// <summary>Deserializes an instance of <typeparamref name="T"/> by matching each json property name against the type's public properties, ignoring the description comments this converter writes.</summary>
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException($"Unexpected token type: {reader.TokenType}");
            }

            var instance = new T();
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return instance;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString();
                    reader.Read(); // Move to value token

                    foreach (var prop in properties)
                    {
                        var jsonName = prop.Name;
                        var jsonPropAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
                        if (jsonPropAttr != null)
                        {
                            jsonName = jsonPropAttr.Name;
                        }

                        if (jsonName == propertyName)
                        {
                            var value = JsonSerializer.Deserialize(ref reader, prop.PropertyType, options);
                            prop.SetValue(instance, value);
                            break;
                        }
                    }
                }
            }

            return instance;
        }

        /// <summary>Writes each property of <paramref name="value"/> as json, prefixing it with a `/* ... */` comment when the property carries a <see cref="DescriptionAttribute"/> — this is what makes the settings file self-documenting.</summary>
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            var prefix = new string(options.IndentCharacter, writer.CurrentDepth * options.IndentSize);

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                var jsonPropertyName = GetJsonPropertyName(property);
                var propertyValue = property.GetValue(value);

                // Сериализуем значение в строку
                var valueJson = JsonSerializer.SerializeToUtf8Bytes(propertyValue, property.PropertyType, options);
                var valueString = System.Text.Encoding.UTF8.GetString(valueJson);

                var descriptionAttr = property.GetCustomAttribute<DescriptionAttribute>();
                if (descriptionAttr != null)
                {
                    var formattedLine = $"{Environment.NewLine}{prefix}/* {descriptionAttr.Description} */{Environment.NewLine}{prefix}\"{jsonPropertyName}\": {valueString}";
                    writer.WriteRawValue(formattedLine, true);
                }
                else
                {
                    var formattedLine = $"{Environment.NewLine}{prefix}\"{jsonPropertyName}\": {valueString}";
                    writer.WriteRawValue(formattedLine, true);
                }
            }

            writer.WriteEndObject();
        }

        /// <summary>The name a property is written under: its <see cref="JsonPropertyNameAttribute"/> if set, otherwise the CLR property name.</summary>
        private string GetJsonPropertyName(PropertyInfo prop)
        {
            var jsonPropAttr = prop.GetCustomAttribute<JsonPropertyNameAttribute>();
            return jsonPropAttr?.Name ?? prop.Name;
        }
    }
}
