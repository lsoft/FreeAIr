using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FreeAIr.Helper
{
    /// <summary>
    /// Конвертер, который записывает массив float[] в JSON файл в виде
    /// последовательности байт в ПСЕВДОшестнадцатеричной кодировке;
    /// алфавит: 0 = A, 1 = B, 2 = C, ...
    /// </summary>
    public sealed class PseudoX16BlobJsonConverter : JsonConverter<float[]>
    {
        private const int _byteA = 'A';

        public override float[] Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
            )
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Ожидается строка в (псевдо)шестнадцатеричном формате для десериализации float[].");
            }

            var hexSpan = reader.ValueSpan;
            if (hexSpan.Length % 2 != 0)
            {
                throw new JsonException("Длина (псевдо)шестнадцатеричной строки должна быть чётной.");
            }
            if ((hexSpan.Length / 2) % sizeof(float) != 0)
            {
                throw new JsonException("Длина массива байт не кратна размеру float.");
            }

            var bytes = ArrayPool<byte>.Shared.Rent(hexSpan.Length / 2);
            for (int i = 0; i < hexSpan.Length; i += 2)
            {
                var value = ParseHexByte(
                    hexSpan[i],
                    hexSpan[i + 1]
                    );

                bytes[i / 2] = value;
            }

            var floatSpan = MemoryMarshal.Cast<byte, float>(bytes.AsSpan());
            var result = floatSpan.ToArray();

            //no finally for performance reasons
            ArrayPool<byte>.Shared.Return(bytes);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte ParseHexByte(
            byte high,
            byte low
            )
        {
            var h = CharToHex(high);
            var l = CharToHex(low);

            var result = (byte)((h << 4) | l);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CharToHex(byte c)
        {
            var result = c - _byteA;
            return result;
        }

        public override void Write(
            Utf8JsonWriter writer,
            float[] value,
            JsonSerializerOptions options
            )
        {
            var bytes = MemoryMarshal.AsBytes<float>(value.AsSpan());
            var hexLength = bytes.Length * 2;
            var hexChars = ArrayPool<char>.Shared.Rent(hexLength);
            try
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    byte b = bytes[i];
                    hexChars[i * 2 + 0] = (char)(_byteA + (b >> 4));
                    hexChars[i * 2 + 1] = (char)(_byteA + (b & 0xF));
                }

                writer.WriteStringValue(new string(hexChars, 0, hexLength));
            }
            finally
            {
                ArrayPool<char>.Shared.Return(hexChars);
            }
        }
    }

    /// <summary>
    /// Конвертер, который записывает массив float[] в JSON файл в виде
    /// последовательности байт в шестнадцатеричной кодировке, например:
    /// A0B33FFF00EE
    /// </summary>
    public sealed class X16BlobJsonConverter : JsonConverter<float[]>
    {
        private static readonly char[] _hexDigits = "0123456789ABCDEF".ToCharArray();

        public override float[] Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Ожидается строка в шестнадцатеричном формате для десериализации float[].");
            }

            ReadOnlySpan<char> hex = reader.GetString().AsSpan();

            if (hex.Length % 2 != 0)
            {
                throw new JsonException("Длина шестнадцатеричной строки должна быть чётной.");
            }

            int byteCount = hex.Length / 2;
            byte[] bytes = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                for (int i = 0; i < hex.Length; i += 2)
                {
                    bool success = TryParseHexByte(
                        hex[i],
                        hex[i + 1],
                        out byte value
                    );

                    if (!success)
                    {
                        throw new JsonException($"Недопустимая шестнадцатеричная пара: {hex[i]}{hex[i + 1]}");
                    }

                    bytes[i / 2] = value;
                }

                if (bytes.Length % sizeof(float) != 0)
                {
                    throw new JsonException("Длина массива байт не кратна размеру float.");
                }

                var floatSpan = MemoryMarshal.Cast<byte, float>(bytes.AsSpan());
                return floatSpan.ToArray();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bytes);
            }
        }

        private static bool TryParseHexByte(char high, char low, out byte value)
        {
            int h = CharToHex(high);
            int l = CharToHex(low);

            if (h < 0 || l < 0)
            {
                value = 0;
                return false;
            }

            value = (byte)((h << 4) | l);
            return true;
        }

        private static int CharToHex(char c)
        {
            if ((uint)(c - '0') <= 9)
            {
                return c - '0';
            }
            c = (char)(c | 0x20); // to lowercase
            if ((uint)(c - 'a') <= 5)
            {
                return (c - 'a') + 10;
            }
            return -1;
        }

        public override void Write(
            Utf8JsonWriter writer,
            float[] value,
            JsonSerializerOptions options
        )
        {
            var bytes = MemoryMarshal.AsBytes<float>(value.AsSpan());
            int hexLength = bytes.Length * 2;
            char[] hexChars = ArrayPool<char>.Shared.Rent(hexLength);
            try
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    byte b = bytes[i];
                    hexChars[i * 2] = _hexDigits[(b >> 4) & 0xF];
                    hexChars[i * 2 + 1] = _hexDigits[b & 0xF];
                }

                writer.WriteStringValue(new string(hexChars, 0, hexLength));
            }
            finally
            {
                ArrayPool<char>.Shared.Return(hexChars);
            }
        }
    }

    /// <summary>
    /// Конвертер, который записывает массив float[] в JSON файл в виде
    /// BASE64-кодированного массива байт.
    /// </summary>
    public sealed class Base64BlobJsonConverter : JsonConverter<float[]>
    {
        public override float[] Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options
        )
        {
            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException("Ожидается строка в формате Base64 для десериализации float[].");
            }

            string base64 = reader.GetString();

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch (FormatException ex)
            {
                throw new JsonException("Недопустимая Base64-строка.", ex);
            }

            if (bytes.Length % sizeof(float) != 0)
            {
                throw new JsonException("Длина массива байт не кратна размеру float.");
            }

            // Преобразуем байты в float[]
            var floatSpan = MemoryMarshal.Cast<byte, float>(bytes.AsSpan());
            return floatSpan.ToArray();
        }

        public override void Write(
            Utf8JsonWriter writer,
            float[] value,
            JsonSerializerOptions options
        )
        {
            // Преобразуем float[] в ReadOnlySpan<byte>
            var bytes = MemoryMarshal.AsBytes<float>(value.AsSpan());

            // Кодируем в Base64
            string base64 = Convert.ToBase64String(bytes.ToArray());

            // Записываем в JSON
            writer.WriteStringValue(base64);
        }
    }

    public sealed class CompactJsonConverter<T> : JsonConverter<T>
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Проверяем, что текущий токен — это объект или значение, которое можно десериализовать
            if (reader.TokenType != JsonTokenType.StartObject && reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Unexpected token type: {reader.TokenType}");
            }

            // Парсим JSON-значение в JsonDocument
            using (JsonDocument document = JsonDocument.ParseValue(ref reader))
            {
                // Десериализуем корневой элемент документа в целевой тип T
                return JsonSerializer.Deserialize<T>(document.RootElement.GetRawText(), options);
            }
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            // Сериализуем значение без отступов (компактный формат)
            var compactOptions = new JsonSerializerOptions(options)
            {
                WriteIndented = false // Отключаем форматирование
            };

            var json = JsonSerializer.SerializeToUtf8Bytes(value, typeof(T), compactOptions);
            writer.WriteRawValue(json);
        }
    }
}
