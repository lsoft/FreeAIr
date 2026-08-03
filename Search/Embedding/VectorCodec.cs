using System;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// Encodes and decodes embedding vectors for the `.embeddings.jsonl` file.
    ///
    /// A vector is stored as int8 scalar quantization in base64. Compared to the float32
    /// pseudo-hex encoding used by FreeAIr 4.2 and earlier this is 6 times smaller, at a measured
    /// cost of |cos| error below 6e-3 — two orders of magnitude less than the gap between
    /// meaningful scores, and the shortlist it produces is handed to an LLM which rechecks
    /// everything anyway.
    ///
    /// The per-vector quantization scale is deliberately not stored: every vector is L2
    /// normalized before use, and normalization cancels any positive scale out.
    /// </summary>
    public static class VectorCodec
    {
        /// <summary>
        /// Written into the metadata json so that a reader can tell what it is looking at, and so
        /// that the encoding can be changed later without breaking the readers already shipped.
        /// </summary>
        public const string Int8Base64EncodingName = "int8-b64-v1";

        /// <summary>The encoding of the legacy `.embeddings.json` file: float32 as pseudo-hex.</summary>
        public const string LegacyFloat32EncodingName = "pseudohex-f32";

        private const int _maxQuantizedValue = 127;

        public static string Encode(
            float[] vector
            )
        {
            if (vector is null)
            {
                throw new ArgumentNullException(nameof(vector));
            }

            if (vector.Length == 0)
            {
                return string.Empty;
            }

            var maxAbs = 0f;
            for (var i = 0; i < vector.Length; i++)
            {
                var a = Math.Abs(vector[i]);
                if (a > maxAbs)
                {
                    maxAbs = a;
                }
            }

            if (maxAbs <= float.Epsilon)
            {
                //a zero vector carries no direction and cannot be normalized
                return string.Empty;
            }

            var scale = maxAbs / _maxQuantizedValue;

            var quantized = new byte[vector.Length];
            for (var i = 0; i < vector.Length; i++)
            {
                var q = (int)Math.Round(vector[i] / scale);
                if (q > _maxQuantizedValue)
                {
                    q = _maxQuantizedValue;
                }
                else if (q < -_maxQuantizedValue)
                {
                    q = -_maxQuantizedValue;
                }

                quantized[i] = unchecked((byte)(sbyte)q);
            }

            return Convert.ToBase64String(quantized);
        }

        /// <summary>
        /// Decodes a vector and normalizes it, so that a cosine similarity against another decoded
        /// vector is a plain dot product. Returns null for an empty or unparsable payload.
        /// </summary>
        public static float[]? DecodeNormalized(
            string? encoded
            )
        {
            if (string.IsNullOrEmpty(encoded))
            {
                return null;
            }

            byte[] quantized;
            try
            {
                quantized = Convert.FromBase64String(encoded);
            }
            catch (FormatException)
            {
                return null;
            }

            if (quantized.Length == 0)
            {
                return null;
            }

            var result = new float[quantized.Length];
            for (var i = 0; i < quantized.Length; i++)
            {
                result[i] = unchecked((sbyte)quantized[i]);
            }

            return NormalizeInPlace(result)
                ? result
                : null
                ;
        }

        /// <summary>
        /// Scales the vector to unit length. Returns false when the vector has no length at all,
        /// in which case it cannot take part in a cosine comparison and has to be dropped.
        /// </summary>
        public static bool NormalizeInPlace(
            float[] vector
            )
        {
            if (vector is null)
            {
                throw new ArgumentNullException(nameof(vector));
            }

            var sum = 0d;
            for (var i = 0; i < vector.Length; i++)
            {
                sum += (double)vector[i] * vector[i];
            }

            if (sum <= double.Epsilon)
            {
                return false;
            }

            var inverted = (float)(1d / Math.Sqrt(sum));
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] *= inverted;
            }

            return true;
        }

        /// <summary>
        /// Cosine similarity of two already normalized vectors, i.e. their dot product. Unrolled
        /// by four: this runs over every vector of the index for every search.
        /// </summary>
        public static float DotProduct(
            float[] left,
            float[] right
            )
        {
            if (left is null)
            {
                throw new ArgumentNullException(nameof(left));
            }

            if (right is null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            var length = Math.Min(left.Length, right.Length);

            var sum0 = 0f;
            var sum1 = 0f;
            var sum2 = 0f;
            var sum3 = 0f;

            var i = 0;
            for (; i <= length - 4; i += 4)
            {
                sum0 += left[i] * right[i];
                sum1 += left[i + 1] * right[i + 1];
                sum2 += left[i + 2] * right[i + 2];
                sum3 += left[i + 3] * right[i + 3];
            }

            var tail = 0f;
            for (; i < length; i++)
            {
                tail += left[i] * right[i];
            }

            return sum0 + sum1 + sum2 + sum3 + tail;
        }
    }
}
