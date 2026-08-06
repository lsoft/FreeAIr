using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.NLOutline.Json;
using FreeAIr.NLOutline.Tree;

namespace FreeAIr.Search.Tests
{
    /// <summary>
    /// Builds an <see cref="EmbeddingIndex"/> out of a handful of tuples, so that a test can state
    /// the ranking it is about instead of the file format it is not about.
    /// </summary>
    internal static class TestIndex
    {
        /// <summary>One node of the index: where it lives, what it says, and where it points in space.</summary>
        internal record struct Node(
            string RelativePath,
            string Target,
            string OutlineText,
            float[] Vector
            );

        /// <summary>
        /// A unit vector whose cosine against (1, 0) is exactly <paramref name="score"/>. Lets a
        /// test write down the score it wants rather than the geometry which produces it.
        /// </summary>
        internal static float[] OfScore(
            float score
            )
        {
            return new[] { score, (float)Math.Sqrt(1d - (score * (double)score)) };
        }

        /// <summary>
        /// A corpus which has nothing to do with the query: <paramref name="count"/> nodes whose
        /// scores are spread evenly between <paramref name="from"/> and <paramref name="to"/>. This
        /// is the background a per-query threshold is measured against, and it is large on purpose —
        /// see <see cref="ScoreDistribution.MinimumSampleSize"/>.
        /// </summary>
        internal static Node[] Background(
            int count,
            float from = 0.30f,
            float to = 0.40f
            )
        {
            var result = new Node[count];

            for (var i = 0; i < count; i++)
            {
                var score = from + ((to - from) * i / (count - 1));

                result[i] = new Node($"Noise\\N{i}.cs", $"N{i}", $"n{i}", OfScore(score));
            }

            return result;
        }

        internal static EmbeddingIndex Build(
            params Node[] nodes
            )
        {
            return Build(null, null, nodes);
        }

        internal static EmbeddingIndex Build(
            EmbeddingCalibration? calibration = null,
            EmbeddingSpaceFingerprint? fingerprint = null,
            Node[]? nodes = null
            )
        {
            nodes ??= Array.Empty<Node>();

            var outlines = new List<OutlineItselfJsonObject>();
            var vectors = new List<EmbeddingItselfJsonObject>();

            foreach (var node in nodes)
            {
                var id = OutlineNode.GenerateGuid(
                    OutlineKindEnum.ClassOrSimilarEntity,
                    node.Target,
                    node.RelativePath
                    );

                outlines.Add(
                    new OutlineItselfJsonObject
                    {
                        Id = id,
                        Kind = OutlineKindEnum.ClassOrSimilarEntity,
                        RelativePath = node.RelativePath,
                        Target = node.Target,
                        OutlineText = node.OutlineText
                    }
                    );

                var vector = (float[])node.Vector.Clone();
                VectorCodec.NormalizeInPlace(vector);

                vectors.Add(new EmbeddingItselfJsonObject(id, vector));
            }

            return EmbeddingIndex.Build(
                new EmbeddingIndexMetadata(
                    "index.json",
                    new DateTime(2026, 1, 1),
                    "the-agent",
                    "the-model",
                    0,
                    "the-model-as-the-server-calls-it",
                    fingerprint,
                    calibration
                    ),
                outlines,
                vectors
                );
        }
    }
}
