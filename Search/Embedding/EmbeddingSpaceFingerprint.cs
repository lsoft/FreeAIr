using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// A few vectors of fixed sentences, stored in the index so that a search can tell whether the
    /// model it is holding now is the one which built the index.
    ///
    /// Neither of the two obvious answers works. The model name is whatever the user typed into the
    /// settings, and a local server usually gets a placeholder — koboldcpp reports its only model as
    /// `inactive` no matter which one is loaded. The length of the vector is not enough either:
    /// mxbai-embed-large and bge-m3 are both 1024, so swapping one for the other leaves an index
    /// which reads perfectly and matches nothing meaningful, silently.
    ///
    /// Comparing the vectors themselves has neither problem: two models agree on these sentences
    /// only if they are the same model.
    /// </summary>
    public sealed class EmbeddingSpaceFingerprint
    {
        /// <summary>
        /// Below this the two are treated as different models. Quantization alone costs about 2e-4,
        /// and two builds of one model land above 0.999, while two different models score far lower
        /// — there is nothing in between to be careful about.
        /// </summary>
        public const float SameSpaceThreshold = 0.99f;

        /// <summary>
        /// The sentences behind the vectors. Changing this list invalidates every fingerprint ever
        /// written, so it is fixed forever: two languages and one line of code, because a model
        /// which treats those alike is not one we can tell apart anyway.
        /// </summary>
        public static readonly IReadOnlyList<string> SentinelTexts = new[]
        {
            "the quick brown fox jumps over the lazy dog",
            "производительность алгоритма сортировки массива",
            "public static void Main(string[] args)",
        };

        private readonly List<float[]> _vectors;

        /// <summary>One normalized vector per <see cref="SentinelTexts"/> entry, in that order.</summary>
        public IReadOnlyList<float[]> Vectors => _vectors;

        public int Dimensions => _vectors.Count > 0 ? _vectors[0].Length : 0;

        private EmbeddingSpaceFingerprint(
            List<float[]> vectors
            )
        {
            _vectors = vectors;
        }

        /// <summary>
        /// Asks the model for the sentinels. Returns null when it answers with anything other than
        /// one usable vector per sentence — a fingerprint which is not complete is worse than none,
        /// because it would refuse searches on an index which is in fact fine.
        /// </summary>
        public static async Task<EmbeddingSpaceFingerprint?> BuildAsync(
            IEmbeddingVectorizer vectorizer,
            CancellationToken cancellationToken
            )
        {
            if (vectorizer is null)
            {
                throw new ArgumentNullException(nameof(vectorizer));
            }

            var vectors = await vectorizer.VectorizeAsync(
                SentinelTexts,
                cancellationToken
                );

            return FromVectors(vectors);
        }

        /// <summary>
        /// Takes raw vectors of the sentinels — normalizing copies, so the caller keeps its own.
        /// Null when there is not exactly one usable vector per sentinel.
        /// </summary>
        public static EmbeddingSpaceFingerprint? FromVectors(
            IReadOnlyList<float[]>? vectors
            )
        {
            if (vectors is null || vectors.Count != SentinelTexts.Count)
            {
                return null;
            }

            var normalized = new List<float[]>(vectors.Count);

            foreach (var vector in vectors)
            {
                if (vector is null || vector.Length == 0)
                {
                    return null;
                }

                var copy = (float[])vector.Clone();
                if (!VectorCodec.NormalizeInPlace(copy))
                {
                    return null;
                }

                normalized.Add(copy);
            }

            return new EmbeddingSpaceFingerprint(normalized);
        }

        /// <summary>
        /// Restores a fingerprint written into the metadata file. Null when the field is absent or
        /// damaged, which is also what an index built before fingerprints existed looks like.
        /// </summary>
        public static EmbeddingSpaceFingerprint? FromEncoded(
            IReadOnlyList<string>? encoded
            )
        {
            if (encoded is null || encoded.Count != SentinelTexts.Count)
            {
                return null;
            }

            var vectors = new List<float[]>(encoded.Count);

            foreach (var payload in encoded)
            {
                var vector = VectorCodec.DecodeNormalized(payload);
                if (vector is null)
                {
                    return null;
                }

                vectors.Add(vector);
            }

            return new EmbeddingSpaceFingerprint(vectors);
        }

        /// <summary>
        /// The form stored in the metadata json: the same int8 base64 as the index itself, which
        /// makes the whole thing about a kilobyte and keeps it stable across rebuilds — the
        /// quantization absorbs the last bits of float noise a model may produce.
        /// </summary>
        public List<string> Encode()
        {
            return _vectors.ConvertAll(VectorCodec.Encode);
        }

        /// <summary>
        /// How much the model behind <paramref name="vectors"/> agrees with the one behind this
        /// fingerprint: the worst of the per-sentence similarities, so that one sentence going
        /// astray is enough to raise the alarm. Returns -1 when the two cannot be compared at all.
        /// </summary>
        public float SimilarityTo(
            IReadOnlyList<float[]>? vectors
            )
        {
            var other = FromVectors(vectors);
            if (other is null)
            {
                return -1f;
            }

            var worst = 1f;

            for (var i = 0; i < _vectors.Count; i++)
            {
                if (_vectors[i].Length != other._vectors[i].Length)
                {
                    return -1f;
                }

                var similarity = VectorCodec.DotProduct(_vectors[i], other._vectors[i]);
                if (similarity < worst)
                {
                    worst = similarity;
                }
            }

            return worst;
        }

        public bool IsSameSpaceAs(
            IReadOnlyList<float[]>? vectors
            )
        {
            return SimilarityTo(vectors) >= SameSpaceThreshold;
        }
    }
}
