using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.NLOutline.Json;
using FreeAIr.NLOutline.Tree;
using Xunit;

namespace FreeAIr.Rag.Tests
{
    public sealed class EmbeddingIndexFacts
    {
        [Fact]
        public void A_vector_whose_node_is_gone_is_dropped()
        {
            //the two files have been regenerated separately, or a merge has kept one side of each.
            //A hit which cannot be traced back to a file is of no use to anybody.
            var index = Build(
                outlines: new[]
                {
                    Outline(OutlineKindEnum.ClassOrSimilarEntity, "A\\Thing.cs", "Thing", "a thing"),
                },
                vectors: new[]
                {
                    Vector(OutlineKindEnum.ClassOrSimilarEntity, "A\\Thing.cs", "Thing", 1f, 0f),
                    Vector(OutlineKindEnum.ClassOrSimilarEntity, "A\\Ghost.cs", "Ghost", 0f, 1f),
                }
                );

            Assert.Single(index.Entries);
            Assert.Equal("Thing", index.Entries[0].Target);
        }

        [Fact]
        public void A_file_with_no_vectors_is_still_covered()
        {
            //"this file is in the index and did not match" and "this file was never indexed" are
            //worth very different things to the user
            var index = Build(
                outlines: new[]
                {
                    Outline(OutlineKindEnum.File, "A\\Empty.cs", "A\\Empty.cs", string.Empty),
                },
                vectors: Array.Empty<EmbeddingItselfJsonObject>()
                );

            Assert.Empty(index.Entries);
            Assert.True(index.IsCovered("A\\Empty.cs"));
            Assert.True(index.IsCovered("a\\empty.cs"), "solution item paths differ in case");
            Assert.False(index.IsCovered("A\\Other.cs"));
            Assert.False(index.IsCovered(string.Empty));
        }

        [Fact]
        public void The_best_match_comes_first()
        {
            var index = Build(
                outlines: new[]
                {
                    Outline(OutlineKindEnum.ClassOrSimilarEntity, "A\\Near.cs", "Near", "near"),
                    Outline(OutlineKindEnum.ClassOrSimilarEntity, "A\\Far.cs", "Far", "far"),
                    Outline(OutlineKindEnum.ClassOrSimilarEntity, "A\\Away.cs", "Away", "away"),
                },
                vectors: new[]
                {
                    Vector(OutlineKindEnum.ClassOrSimilarEntity, "A\\Near.cs", "Near", 1f, 0.1f),
                    Vector(OutlineKindEnum.ClassOrSimilarEntity, "A\\Far.cs", "Far", 0.5f, 1f),
                    Vector(OutlineKindEnum.ClassOrSimilarEntity, "A\\Away.cs", "Away", -1f, 0f),
                }
                );

            var found = index.Search(new[] { 1f, 0f }, 10);

            Assert.Equal(
                new[] { "Near", "Far", "Away" },
                found.Select(f => f.Entry.Target)
                );
            Assert.True(found[0].Score > found[1].Score);
            Assert.True(found[2].Score < 0f);
        }

        [Fact]
        public void Only_the_head_of_the_ranking_is_returned()
        {
            var index = Build(
                outlines: Enumerable.Range(0, 10)
                    .Select(i => Outline(OutlineKindEnum.ClassOrSimilarEntity, $"A\\T{i}.cs", $"T{i}", $"t{i}"))
                    .ToArray(),
                vectors: Enumerable.Range(0, 10)
                    .Select(i => Vector(OutlineKindEnum.ClassOrSimilarEntity, $"A\\T{i}.cs", $"T{i}", 1f, i * 0.1f))
                    .ToArray()
                );

            Assert.Equal(3, index.Search(new[] { 1f, 0f }, 3).Count);
            Assert.Empty(index.Search(new[] { 1f, 0f }, 0));
        }

        [Fact]
        public void Equal_scores_come_out_in_the_same_order_every_time()
        {
            //List.Sort is not stable, and outlines which score exactly the same are common. An
            //unstable order would make the same query return a different shortlist each run.
            var index = Build(
                outlines: Enumerable.Range(0, 30)
                    .Select(i => Outline(OutlineKindEnum.ClassOrSimilarEntity, $"A\\T{i}.cs", $"T{i}", "same"))
                    .ToArray(),
                vectors: Enumerable.Range(0, 30)
                    .Select(i => Vector(OutlineKindEnum.ClassOrSimilarEntity, $"A\\T{i}.cs", $"T{i}", 1f, 0f))
                    .ToArray()
                );

            var first = index.Search(new[] { 1f, 0f }, 30).Select(f => f.Entry.Id).ToList();
            var second = index.Search(new[] { 1f, 0f }, 30).Select(f => f.Entry.Id).ToList();

            Assert.Equal(first, second);
            Assert.Equal(first, first.OrderBy(id => id).ToList());
        }

        [Fact]
        public void A_vector_of_another_length_is_not_compared_against()
        {
            //another length means another model, and a dot product of the two is not a similarity
            //of anything
            var index = Build(
                outlines: new[]
                {
                    Outline(OutlineKindEnum.ClassOrSimilarEntity, "A\\Short.cs", "Short", "short"),
                },
                vectors: new[]
                {
                    Vector(OutlineKindEnum.ClassOrSimilarEntity, "A\\Short.cs", "Short", 1f, 0f),
                }
                );

            Assert.Empty(index.Search(new[] { 1f, 0f, 0f }, 10));
        }

        [Fact]
        public void A_query_with_no_direction_matches_nothing()
        {
            var index = Build(
                outlines: new[]
                {
                    Outline(OutlineKindEnum.ClassOrSimilarEntity, "A\\Thing.cs", "Thing", "a thing"),
                },
                vectors: new[]
                {
                    Vector(OutlineKindEnum.ClassOrSimilarEntity, "A\\Thing.cs", "Thing", 1f, 0f),
                }
                );

            Assert.Empty(index.Search(new[] { 0f, 0f }, 10));
        }

        [Fact]
        public void The_scan_can_be_cancelled()
        {
            var index = Build(
                outlines: Enumerable.Range(0, 5000)
                    .Select(i => Outline(OutlineKindEnum.ClassOrSimilarEntity, $"A\\T{i}.cs", $"T{i}", "x"))
                    .ToArray(),
                vectors: Enumerable.Range(0, 5000)
                    .Select(i => Vector(OutlineKindEnum.ClassOrSimilarEntity, $"A\\T{i}.cs", $"T{i}", 1f, i))
                    .ToArray()
                );

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.Throws<OperationCanceledException>(
                () => index.Search(new[] { 1f, 0f }, 10, cts.Token)
                );
        }

        [Fact]
        public void The_dimensions_are_taken_from_the_vectors_themselves()
        {
            var index = Build(
                outlines: new[]
                {
                    Outline(OutlineKindEnum.ClassOrSimilarEntity, "A\\Thing.cs", "Thing", "a thing"),
                },
                vectors: new[]
                {
                    Vector(OutlineKindEnum.ClassOrSimilarEntity, "A\\Thing.cs", "Thing", 1f, 0f, 0f),
                },
                declaredDimensions: 1536
                );

            //the header of a hand edited or half merged file can say anything; the vectors cannot
            Assert.Equal(3, index.Dimensions);
        }

        private static EmbeddingIndex Build(
            IReadOnlyList<OutlineItselfJsonObject> outlines,
            IReadOnlyList<EmbeddingItselfJsonObject> vectors,
            int declaredDimensions = 0
            )
        {
            return EmbeddingIndex.Build(
                new EmbeddingIndexMetadata(
                    "index.json",
                    new DateTime(2026, 1, 1),
                    "the-agent",
                    "the-model",
                    declaredDimensions
                    ),
                outlines,
                vectors
                );
        }

        private static OutlineItselfJsonObject Outline(
            OutlineKindEnum kind,
            string relativePath,
            string target,
            string outlineText
            )
        {
            return new OutlineItselfJsonObject
            {
                Id = OutlineNode.GenerateGuid(kind, target, relativePath),
                Kind = kind,
                RelativePath = relativePath,
                Target = target,
                OutlineText = outlineText
            };
        }

        private static EmbeddingItselfJsonObject Vector(
            OutlineKindEnum kind,
            string relativePath,
            string target,
            params float[] vector
            )
        {
            VectorCodec.NormalizeInPlace(vector);

            return new EmbeddingItselfJsonObject(
                OutlineNode.GenerateGuid(kind, target, relativePath),
                vector
                );
        }
    }
}
