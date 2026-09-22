using System.Text.Json;

namespace FreeAIr.Llm.Wire
{
    /// <summary>
    /// Digs the sentence the user can act on out of a failed response body.
    ///
    /// Every provider wraps its complaint differently and none of them puts it in the HTTP status,
    /// which is all an exception message carries. OpenAI and vLLM use `error.message`, some proxies
    /// a top level `message`, Anthropic `error.message` as well; a gateway which fell over sends
    /// HTML and nothing can be extracted at all.
    /// </summary>
    public static class ServerErrorMessageReader
    {
        /// <summary>
        /// How much of a non-JSON error page is kept. The useful sentence is at the front; the rest
        /// of a gateway HTML dump only buries it.
        /// </summary>
        public const int MaxReportedBodyLength = 4000;

        /// <summary>
        /// The complaint inside the body, or the trimmed body itself when it is not JSON. Null for
        /// a body which is empty or absent.
        /// </summary>
        public static string? Read(
            string? body
            )
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            var trimmed = body!.Trim();

            var fromJson = TryReadJsonErrorMessage(trimmed);
            if (!string.IsNullOrWhiteSpace(fromJson))
            {
                return fromJson;
            }

            if (trimmed.Length > MaxReportedBodyLength)
            {
                return trimmed.Substring(0, MaxReportedBodyLength) + "...";
            }

            return trimmed;
        }

        /// <summary>
        /// Pulls `error.message` (OpenAI / vLLM / Anthropic) or a top-level `message` out of the
        /// response body so the chat shows the sentence the user can act on, not the whole JSON
        /// envelope.
        /// </summary>
        private static string? TryReadJsonErrorMessage(
            string body
            )
        {
            try
            {
                using var json = JsonDocument.Parse(body);
                if (json.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                if (json.RootElement.TryGetProperty("error", out var error)
                    && error.ValueKind == JsonValueKind.Object
                    && error.TryGetProperty("message", out var nested)
                    && nested.ValueKind == JsonValueKind.String)
                {
                    return nested.GetString();
                }

                if (json.RootElement.TryGetProperty("message", out var message)
                    && message.ValueKind == JsonValueKind.String)
                {
                    return message.GetString();
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
