using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace FreeAIr.Llm.OpenAi
{
    /// <summary>
    /// Picks the reasoning of a thinking model out of an OpenAI compatible event stream, reading
    /// the bytes as they go past on their way to the SDK.
    ///
    /// It exists because the SDK cannot report what it does not model: a chunk is deserialized into
    /// `StreamingChatCompletionUpdate`, which has no property for reasoning, and a property the
    /// wire-format read does not recognise is dropped rather than kept. Nor is there a release to
    /// wait for - the field was never part of the protocol. DeepSeek, vLLM and llama.cpp put it in
    /// `reasoning_content`, OpenRouter in `reasoning`, and OpenAI's own o-series does not send it at
    /// all. Both spellings are read here and anything which is not a string is ignored.
    ///
    /// Reading the body a second time is the price of keeping the SDK, and the SDK is what builds
    /// the request every `OpenAiRequestFacts` case asserts.
    /// </summary>
    internal sealed class ReasoningSseScanner
    {
        /// <summary>
        /// How much of a line is carried between reads before it is given up on. A server-sent
        /// event is one line and a reasoning chunk is a sentence; a megabyte without a line break
        /// is not this protocol, and remembering it forever would be a leak on a stream which never
        /// ends.
        /// </summary>
        private const int MaxPendingLineLength = 1024 * 1024;

        /// <summary>
        /// Decodes across read boundaries: a chunk of bytes may end in the middle of a character,
        /// and a decoder of its own per read would turn that into a replacement character in the
        /// middle of a word.
        /// </summary>
        private readonly Decoder _decoder = new UTF8Encoding(false).GetDecoder();

        /// <summary>The tail of the last read: everything after its final line break.</summary>
        private readonly StringBuilder _pending = new StringBuilder();

        /// <summary>
        /// Takes the next bytes of the stream and returns the reasoning fragments they completed.
        /// A read which finished no line yields nothing and is not lost - the tail is kept for the
        /// next one.
        /// </summary>
        public IReadOnlyList<string> Append(
            byte[] buffer,
            int offset,
            int count
            )
        {
            if (buffer is null || count <= 0)
            {
                return Array.Empty<string>();
            }

            var chars = new char[_decoder.GetCharCount(buffer, offset, count, false)];
            var charCount = _decoder.GetChars(buffer, offset, count, chars, 0, false);

            List<string>? found = null;

            for (var i = 0; i < charCount; i++)
            {
                var c = chars[i];
                if (c != '\n')
                {
                    if (_pending.Length < MaxPendingLineLength)
                    {
                        _pending.Append(c);
                    }
                    continue;
                }

                var reasoning = ReadLine(_pending.ToString());
                _pending.Clear();

                if (reasoning is not null)
                {
                    (found ??= new List<string>()).Add(reasoning);
                }
            }

            return (IReadOnlyList<string>?)found ?? Array.Empty<string>();
        }

        /// <summary>
        /// The reasoning in the last line of the stream, if it was never terminated by a line break.
        /// Called once the stream is over; a scanner which is not flushed loses the final chunk of
        /// a server which does not end its body with a newline.
        /// </summary>
        public IReadOnlyList<string> Flush()
        {
            if (_pending.Length == 0)
            {
                return Array.Empty<string>();
            }

            var reasoning = ReadLine(_pending.ToString());
            _pending.Clear();

            return reasoning is null
                ? Array.Empty<string>()
                : new[] { reasoning };
        }

        /// <summary>
        /// The reasoning inside one line of the body, or null for the many lines which carry none:
        /// the blank line between events, the `event:` and `id:` lines, the `[DONE]` sentinel, and
        /// every chunk which is only answer text or a tool call.
        /// </summary>
        private static string? ReadLine(
            string line
            )
        {
            var trimmed = line.TrimEnd('\r');
            if (!trimmed.StartsWith("data:"))
            {
                return null;
            }

            var payload = trimmed.Substring("data:".Length).TrimStart(' ');
            if (payload.Length == 0 || payload == "[DONE]")
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(payload);
                return ReadReasoning(document.RootElement);
            }
            catch (JsonException)
            {
                //a line which is not json is either an error body or a proxy rewriting the stream,
                //and the SDK reading the same bytes reports it as a fault of its own; complaining
                //here as well would double every such report
                return null;
            }
        }

        /// <summary>The `reasoning_content` or `reasoning` of the first choice which has one.</summary>
        private static string? ReadReasoning(
            JsonElement root
            )
        {
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var choice in choices.EnumerateArray())
            {
                if (choice.ValueKind != JsonValueKind.Object
                    || !choice.TryGetProperty("delta", out var delta)
                    || delta.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var reasoning = ReadString(delta, "reasoning_content")
                    ?? ReadString(delta, "reasoning");

                if (!string.IsNullOrEmpty(reasoning))
                {
                    return reasoning;
                }
            }

            return null;
        }

        /// <summary>The named property when it is a string, and null for a null, an object or a missing one.</summary>
        private static string? ReadString(
            JsonElement owner,
            string name
            )
        {
            return owner.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
    }
}
