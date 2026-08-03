using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.Find;
using FreeAIr.NLOutline.Json;
using FreeAIr.NLOutline.Tree;
using Xunit;

namespace FreeAIr.Search.Tests
{
    /// <summary>
    /// The measurement which replaces the cosine threshold nobody could pick: the index is asked a
    /// few questions with no answer and a few whose answer is known, and the threshold is derived
    /// from where the two land.
    /// </summary>
    public sealed class RagCalibrationFacts
    {
        [Fact]
        public async Task The_noise_ceiling_is_the_best_a_hopeless_query_can_do()
        {
            var index = BuildIndex(
                ("A\\Search.cs", "Search", "searches for voyages", new[] { 1f, 0f, 0f })
                );

            var vectorizer = new FakeVectorizer(3);
            //one nonsense query which lands nowhere, one which half rhymes with the outline
            vectorizer.Add("far", 0f, 1f, 0f);
            vectorizer.Add("near", 0.6f, 0.8f, 0f);

            var calibration = await RagCalibrator.RunAsync(
                index,
                vectorizer,
                new RagCalibrationProbes(irrelevant: new[] { "far", "near" }),
                default
                );

            Assert.NotNull(calibration);
            Assert.Equal(0.6f, calibration!.NoiseCeiling, 3);
            Assert.Equal(2, calibration.IrrelevantProbeCount);
        }

        [Fact]
        public async Task The_relevant_floor_is_the_worst_a_known_answer_can_do()
        {
            var index = BuildIndex(
                ("A\\First.cs", "First", "the first thing", new[] { 1f, 0f, 0f }),
                ("A\\Second.cs", "Second", "the second thing", new[] { 0f, 1f, 0f })
                );

            var vectorizer = new FakeVectorizer(3);
            vectorizer.Add("nothing", 0f, 0f, 1f);
            vectorizer.Add("about the first", 1f, 0f, 0f);
            vectorizer.Add("about the second", 0.6f, 0.8f, 0f);

            var calibration = await RagCalibrator.RunAsync(
                index,
                vectorizer,
                new RagCalibrationProbes(
                    relevant: new[]
                    {
                        new RagRelevantProbe("about the first", "A\\First.cs"),
                        new RagRelevantProbe("about the second", "A\\Second.cs"),
                    },
                    irrelevant: new[] { "nothing" }
                    ),
                default
                );

            Assert.NotNull(calibration);
            Assert.Equal(0.8f, calibration!.RelevantFloor, 3);
            Assert.Equal(2, calibration.RelevantProbeCount);
            Assert.Equal(0, calibration.RelevantMissCount);
            Assert.True(calibration.ModelSeparates);
        }

        [Fact]
        public async Task A_labelled_query_which_does_not_find_its_file_is_a_miss()
        {
            //not a low score: the model is wrong about this codebase, and a threshold cannot fix
            //that. It has to be counted separately so the user is told to change the model.
            var index = BuildIndex(
                ("A\\First.cs", "First", "the first thing", new[] { 1f, 0f, 0f })
                );

            var vectorizer = new FakeVectorizer(3);
            vectorizer.Add("nothing", 0f, 0f, 1f);
            vectorizer.Add("about a file which is not indexed", 1f, 0f, 0f);

            var calibration = await RagCalibrator.RunAsync(
                index,
                vectorizer,
                new RagCalibrationProbes(
                    relevant: new[]
                    {
                        new RagRelevantProbe("about a file which is not indexed", "A\\Missing.cs"),
                    },
                    irrelevant: new[] { "nothing" }
                    ),
                default
                );

            Assert.NotNull(calibration);
            Assert.Equal(1, calibration!.RelevantMissCount);
            Assert.Equal(1, calibration.RelevantProbeCount);
            Assert.Equal(0f, calibration.RelevantFloor);
        }

        [Fact]
        public async Task A_model_which_scores_nonsense_as_high_as_the_answers_is_reported()
        {
            var index = BuildIndex(
                ("A\\First.cs", "First", "the first thing", new[] { 1f, 0f, 0f })
                );

            var vectorizer = new FakeVectorizer(3);
            //the hopeless query scores higher than the query whose answer is known
            vectorizer.Add("nothing", 1f, 0f, 0f);
            vectorizer.Add("about the first", 0.6f, 0.8f, 0f);

            var calibration = await RagCalibrator.RunAsync(
                index,
                vectorizer,
                new RagCalibrationProbes(
                    relevant: new[] { new RagRelevantProbe("about the first", "A\\First.cs") },
                    irrelevant: new[] { "nothing" }
                    ),
                default
                );

            Assert.NotNull(calibration);
            Assert.False(calibration!.ModelSeparates);
        }

        [Fact]
        public async Task An_empty_index_is_left_uncalibrated()
        {
            var index = BuildIndex();

            var calibration = await RagCalibrator.RunAsync(
                index,
                new FakeVectorizer(3),
                RagCalibrationProbes.Default,
                default
                );

            Assert.Null(calibration);
        }

        [Fact]
        public void An_empty_probe_list_falls_back_to_the_built_in_nonsense()
        {
            //otherwise the feature would only work for users who have opened the settings
            var probes = new RagCalibrationProbes(irrelevant: Array.Empty<string>());

            Assert.Equal(RagCalibrationProbes.DefaultIrrelevant, probes.Irrelevant);
        }

        [Fact]
        public async Task Calibrating_fills_the_metadata_the_index_is_written_from()
        {
            var root = TreeFactory.Solution();
            var file = root.Project("Lib\\Lib.csproj", "Lib").File("Lib\\Thing.cs");
            file.Type("Thing", "a thing which does something");

            var vectorizer = new FakeVectorizer(8);
            await new OutlineEmbedder(vectorizer).GenerateEmbeddingsAsync(root, default);

            var json = new EmbeddingOutlineJsonObject(root, "the-agent", "the-model", "http://localhost");

            await RagCalibrator.ApplyToAsync(
                json,
                vectorizer,
                RagCalibrationProbes.Default,
                default
                );

            Assert.NotNull(json.Calibration);
            Assert.Equal(
                RagCalibrationProbes.DefaultIrrelevant.Count,
                json.Calibration!.IrrelevantProbeCount
                );

            //the name the server gave, not the one the settings asked for
            Assert.Equal("fake-embedding-model-q8", json.ReportedEmbeddingModel);

            var fingerprint = EmbeddingSpaceFingerprint.FromEncoded(json.SpaceFingerprint);
            Assert.NotNull(fingerprint);
            Assert.Equal(
                1f,
                fingerprint!.SimilarityTo(
                    await vectorizer.VectorizeAsync(EmbeddingSpaceFingerprint.SentinelTexts, default)
                    ),
                3
                );
        }

        [Fact]
        public async Task Calibration_stops_when_it_is_cancelled()
        {
            var index = BuildIndex(
                ("A\\First.cs", "First", "the first thing", new[] { 1f, 0f, 0f })
                );

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => RagCalibrator.RunAsync(
                    index,
                    new FakeVectorizer(3),
                    RagCalibrationProbes.Default,
                    cts.Token
                    )
                );
        }

        private static EmbeddingIndex BuildIndex(
            params (string RelativePath, string Target, string OutlineText, float[] Vector)[] nodes
            )
        {
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
                new EmbeddingIndexMetadata("index.json", new DateTime(2026, 1, 1), "the-agent", "the-model", 0),
                outlines,
                vectors
                );
        }
    }
}
