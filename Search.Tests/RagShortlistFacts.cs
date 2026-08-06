using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.Find;
using FreeAIr.NLOutline.Json;
using FreeAIr.NLOutline.Tree;
using Xunit;

namespace FreeAIr.Search.Tests
{
    /// <summary>
    /// The search itself: a query goes in, a handful of files comes out.
    /// </summary>
    public sealed class RagShortlistFacts
    {
        private const string _query = "where do we store the invoices";

        [Fact]
        public async Task The_file_whose_outline_is_closest_to_the_query_wins()
        {
            var index = BuildIndex(
                ("Billing\\Invoice.cs", "Invoice", "an invoice stored in the database", new[] { 1f, 0f, 0f }),
                ("Ui\\Button.cs", "Button", "a button on a toolbar", new[] { 0f, 1f, 0f }),
                ("Net\\Socket.cs", "Socket", "a tcp socket", new[] { 0f, 0f, 1f })
                );

            var result = await RagShortlist.BuildAsync(
                index,
                Vectorizer(new[] { 1f, 0.1f, 0f }),
                _query,
                new RagShortlistOptions(),
                default
                );

            Assert.False(result.ModelMismatch);
            Assert.Equal(3, result.IndexedOutlineCount);
            Assert.Equal("Billing\\Invoice.cs", result.Candidates[0].RelativePath);
            Assert.Equal("Invoice", result.Candidates[0].BestTarget);
            Assert.Equal("an invoice stored in the database", result.Candidates[0].BestOutlineText);
        }

        [Fact]
        public async Task A_file_is_worth_its_best_outline_and_not_the_sum_of_them()
        {
            //summing would reward a long file for having many members, which is not what the user
            //asked about
            var index = BuildIndex(
                ("Big\\Fat.cs", "Fat.A", "a", new[] { 0.6f, 0.8f }),
                ("Big\\Fat.cs", "Fat.B", "b", new[] { 0.6f, 0.8f }),
                ("Big\\Fat.cs", "Fat.C", "c", new[] { 0.6f, 0.8f }),
                ("Small\\Thin.cs", "Thin.A", "a", new[] { 1f, 0f })
                );

            var result = await RagShortlist.BuildAsync(
                index,
                Vectorizer(new[] { 1f, 0f }),
                _query,
                new RagShortlistOptions(),
                default
                );

            Assert.Equal("Small\\Thin.cs", result.Candidates[0].RelativePath);
            Assert.Equal(1, result.Candidates[0].MatchedOutlineCount);

            var fat = result.Candidates.Single(c => c.RelativePath == "Big\\Fat.cs");
            Assert.Equal(3, fat.MatchedOutlineCount);
        }

        [Fact]
        public async Task Nothing_below_the_threshold_gets_in()
        {
            var index = BuildIndex(
                calibration: new EmbeddingCalibration(0.5f, 0f, 5, 0, 0),
                nodes: new[]
                {
                    ("A\\Near.cs", "Near", "near", new[] { 1f, 0f }),
                    ("A\\Far.cs", "Far", "far", new[] { 0.4f, 1f })
                }
                );

            var result = await RagShortlist.BuildAsync(
                index,
                Vectorizer(new[] { 1f, 0f }),
                _query,
                //sensitivity zero puts the threshold exactly at the measured noise
                new RagShortlistOptions { Sensitivity = 0d },
                default
                );

            Assert.Equal(0.5f, result.AppliedMinScore, 3);
            Assert.Equal("A\\Near.cs", Assert.Single(result.Candidates).RelativePath);
        }

        [Fact]
        public async Task The_threshold_is_where_the_sensitivity_puts_it()
        {
            //the setting is a share of the room between the noise and a perfect match, so the same
            //number means the same thing on models whose absolute scales differ
            var calibration = new EmbeddingCalibration(0.5f, 0f, 5, 0, 0);

            Assert.Equal(0.5f, calibration.ComputeThreshold(0d), 3);
            Assert.Equal(0.6f, calibration.ComputeThreshold(0.2d), 3);
            Assert.Equal(0.75f, calibration.ComputeThreshold(0.5d), 3);
        }

        [Fact]
        public async Task An_index_which_was_never_calibrated_keeps_everything()
        {
            //no measurement means no threshold: inventing one would bring back the constant that
            //the calibration replaced
            var index = BuildIndex(
                ("A\\Near.cs", "Near", "near", new[] { 1f, 0f }),
                ("A\\Far.cs", "Far", "far", new[] { 0f, 1f })
                );

            var result = await RagShortlist.BuildAsync(
                index,
                Vectorizer(new[] { 1f, 0f }),
                _query,
                new RagShortlistOptions(),
                default
                );

            Assert.Equal(0f, result.AppliedMinScore);
            Assert.Equal(2, result.Candidates.Count);
        }

        [Fact]
        public async Task A_labelled_answer_is_never_cut_off_by_a_strict_setting()
        {
            //the user has pointed at this file personally; a knob may be strict, but not to the
            //point of dropping it
            var calibration = new EmbeddingCalibration(0.4f, 0.6f, 5, 1, 0);

            Assert.True(calibration.ComputeThreshold(1d) < 0.6f);
            Assert.True(calibration.ComputeThreshold(1d) > 0.4f);
        }

        [Fact]
        public async Task A_probe_shows_what_the_threshold_would_have_cut_off()
        {
            //the calibration window lives on this: a threshold can only be judged by looking at
            //what it throws away
            var index = BuildIndex(
                calibration: new EmbeddingCalibration(0.5f, 0f, 5, 0, 0),
                nodes: new[]
                {
                    ("A\\Near.cs", "Near", "near", new[] { 1f, 0f }),
                    ("A\\Far.cs", "Far", "far", new[] { 0.4f, 1f })
                }
                );

            var options = new RagShortlistOptions { Sensitivity = 0d };

            var searched = await RagShortlist.BuildAsync(
                index,
                Vectorizer(new[] { 1f, 0f }),
                _query,
                options,
                default
                );

            var probed = await RagShortlist.ProbeAsync(
                index,
                Vectorizer(new[] { 1f, 0f }),
                _query,
                options,
                default
                );

            Assert.Single(searched.Candidates);
            Assert.Equal(2, probed.Candidates.Count);

            //the threshold is reported either way, and it is the same number
            Assert.Equal(searched.AppliedMinScore, probed.AppliedMinScore, 3);
            Assert.Equal(0.5f, probed.AppliedMinScore, 3);

            //still ranked, and the row below the threshold is the one the window has to paint
            Assert.Equal("A\\Near.cs", probed.Candidates[0].RelativePath);
            Assert.True(probed.Candidates[1].Score < probed.AppliedMinScore);
        }

        [Fact]
        public async Task A_probe_with_another_model_is_refused_like_a_search()
        {
            //calibrating on numbers produced by the wrong model would be worse than not
            //calibrating at all, so the probe refuses on exactly the same terms
            var builder = new FakeVectorizer(8) { Variant = 0 };
            var fingerprint = await EmbeddingSpaceFingerprint.BuildAsync(builder, default);

            var index = BuildIndex(
                fingerprint: fingerprint,
                nodes: new[]
                {
                    ("A\\Thing.cs", "Thing", "a thing", new[] { 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f })
                }
                );

            var another = new FakeVectorizer(8) { Variant = 3 };
            another.Add(_query, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

            var result = await RagShortlist.ProbeAsync(
                index,
                another,
                _query,
                new RagShortlistOptions(),
                default
                );

            Assert.True(result.SpaceMismatch);
            Assert.Empty(result.Candidates);
        }

        [Fact]
        public async Task A_search_with_another_model_of_the_same_size_is_refused()
        {
            //the vectors are of the right length and mean nothing at all: this is exactly the case
            //the dimension check cannot see
            var builder = new FakeVectorizer(8) { Variant = 0 };
            var fingerprint = await EmbeddingSpaceFingerprint.BuildAsync(builder, default);

            var index = BuildIndex(
                fingerprint: fingerprint,
                nodes: new[]
                {
                    ("A\\Thing.cs", "Thing", "a thing", new[] { 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f })
                }
                );

            var another = new FakeVectorizer(8) { Variant = 3 };
            another.Add(_query, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

            var result = await RagShortlist.BuildAsync(
                index,
                another,
                _query,
                new RagShortlistOptions(),
                default
                );

            Assert.True(result.SpaceMismatch);
            Assert.Empty(result.Candidates);
        }

        [Fact]
        public async Task The_model_which_built_the_index_passes_the_check_in_one_request()
        {
            var vectorizer = new FakeVectorizer(8);
            var fingerprint = await EmbeddingSpaceFingerprint.BuildAsync(vectorizer, default);

            var index = BuildIndex(
                fingerprint: fingerprint,
                nodes: new[]
                {
                    ("A\\Thing.cs", "Thing", "a thing", new[] { 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f })
                }
                );

            var searching = new FakeVectorizer(8);
            searching.Add(_query, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f);

            var result = await RagShortlist.BuildAsync(
                index,
                searching,
                _query,
                new RagShortlistOptions(),
                default
                );

            Assert.False(result.SpaceMismatch);
            Assert.Equal(1f, result.SpaceSimilarity!.Value, 3);
            Assert.Single(result.Candidates);

            //the sentinels travel with the query rather than in a request of their own: the check
            //has to be free, or it would be turned off
            var request = Assert.Single(searching.Requests);
            Assert.Equal(1 + EmbeddingSpaceFingerprint.SentinelTexts.Count, request.Count);
            Assert.Equal(_query, request[0]);
        }

        [Fact]
        public async Task The_shortlist_is_cut_to_the_configured_length()
        {
            var index = BuildIndex(
                Enumerable.Range(0, 20)
                    .Select(i => ($"A\\T{i}.cs", $"T{i}", $"t{i}", new[] { 1f, i * 0.001f }))
                    .ToArray()
                );

            var result = await RagShortlist.BuildAsync(
                index,
                Vectorizer(new[] { 1f, 0f }),
                _query,
                new RagShortlistOptions { MaxFileCount = 4 },
                default
                );

            Assert.Equal(4, result.Candidates.Count);
        }

        [Fact]
        public async Task Only_the_ranked_head_reaches_the_files()
        {
            //TopOutlineCount is the size of the ranking, not of the shortlist: an outline which did
            //not make the head cannot bring its file in
            var index = BuildIndex(
                ("A\\First.cs", "First", "first", new[] { 1f, 0f }),
                ("A\\Second.cs", "Second", "second", new[] { 0.9f, 0.1f })
                );

            var result = await RagShortlist.BuildAsync(
                index,
                Vectorizer(new[] { 1f, 0f }),
                _query,
                new RagShortlistOptions { TopOutlineCount = 1 },
                default
                );

            Assert.Equal("A\\First.cs", Assert.Single(result.Candidates).RelativePath);
        }

        [Fact]
        public async Task A_query_vectorized_by_the_wrong_model_is_reported_rather_than_answered()
        {
            var index = BuildIndex(
                ("A\\Thing.cs", "Thing", "a thing", new[] { 1f, 0f, 0f })
                );

            var result = await RagShortlist.BuildAsync(
                index,
                Vectorizer(new[] { 1f, 0f }),
                _query,
                new RagShortlistOptions(),
                default
                );

            Assert.True(result.ModelMismatch);
            Assert.Equal(2, result.QueryDimensions);
            Assert.Equal(3, result.IndexDimensions);
            Assert.Empty(result.Candidates);
        }

        [Fact]
        public async Task Cancelling_stops_the_search_before_it_asks_the_model()
        {
            var index = BuildIndex(
                ("A\\Thing.cs", "Thing", "a thing", new[] { 1f, 0f })
                );

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => RagShortlist.BuildAsync(
                    index,
                    Vectorizer(new[] { 1f, 0f }),
                    _query,
                    new RagShortlistOptions(),
                    cts.Token
                    )
                );
        }

        [Fact]
        public void Files_the_index_never_saw_are_named()
        {
            //otherwise a RAG search silently ignores everything added since the index was built
            var index = BuildIndex(
                ("A\\Known.cs", "Known", "known", new[] { 1f, 0f })
                );

            var uncovered = RagShortlist.FindUncovered(
                index,
                new[] { "A\\Known.cs", "a\\known.cs", "A\\Brand.New.cs" }
                );

            Assert.Equal(new[] { "A\\Brand.New.cs" }, uncovered);
        }

        [Fact]
        public async Task The_query_is_what_gets_vectorized()
        {
            var index = BuildIndex(
                ("A\\Thing.cs", "Thing", "a thing", new[] { 1f, 0f })
                );

            var vectorizer = new FakeVectorizer(2);
            vectorizer.Add(_query, 1f, 0f);

            await RagShortlist.BuildAsync(index, vectorizer, _query, new RagShortlistOptions(), default);

            Assert.Equal(_query, Assert.Single(Assert.Single(vectorizer.Requests)));
        }

        [Fact]
        public async Task A_query_which_stands_out_of_the_background_passes_without_any_calibration()
        {
            //the scan scores every outline anyway, so the level this query sits at against an
            //arbitrary piece of the corpus costs nothing to measure - and an index which has never
            //been calibrated still gets a threshold out of it
            var index = TestIndex.Build(
                TestIndex.Background(400)
                    .Append(new TestIndex.Node("A\\Answer.cs", "Answer", "the answer", TestIndex.OfScore(0.9f)))
                    .ToArray()
                );

            var result = await RagShortlist.BuildAsync(
                index,
                Vectorizer(new[] { 1f, 0f }),
                _query,
                new RagShortlistOptions(),
                default
                );

            Assert.NotNull(result.Distribution);
            Assert.Equal(
                result.Distribution!.ComputeThreshold(0.2d),
                result.AppliedMinScore,
                4
                );

            Assert.True(result.AppliedMinScore > 0.40f, "the background has to be cut off");
            Assert.Equal("A\\Answer.cs", Assert.Single(result.Candidates).RelativePath);
        }

        [Fact]
        public async Task A_query_the_corpus_cannot_answer_finds_nothing()
        {
            //nothing stands out of the background, and the honest answer to that is an empty
            //shortlist rather than the fifteen files which happened to be least far away
            var index = TestIndex.Build(TestIndex.Background(400));

            var result = await RagShortlist.BuildAsync(
                index,
                Vectorizer(new[] { 1f, 0f }),
                _query,
                new RagShortlistOptions(),
                default
                );

            Assert.True(result.Distribution!.BestDeviations < 2f);
            Assert.Empty(result.Candidates);
        }

        [Fact]
        public async Task A_file_has_to_clear_both_the_calibration_and_the_query()
        {
            //each of the two sees a kind of noise the other one cannot: the calibration knows how
            //high nonsense climbs on this corpus, the distribution knows how close this particular
            //query is to everything
            var nodes = TestIndex.Background(400)
                .Append(new TestIndex.Node("A\\Answer.cs", "Answer", "the answer", TestIndex.OfScore(0.9f)))
                .ToArray();

            var options = new RagShortlistOptions { Sensitivity = 0d };

            var byTheQuery = await RagShortlist.BuildAsync(
                TestIndex.Build(new EmbeddingCalibration(0.1f, 0f, 5, 0, 0), null, nodes),
                Vectorizer(new[] { 1f, 0f }),
                _query,
                options,
                default
                );

            //the calibration is the lower of the two and the query's own background decides
            Assert.Equal(byTheQuery.Distribution!.ComputeThreshold(0d), byTheQuery.AppliedMinScore, 4);
            Assert.Equal("A\\Answer.cs", Assert.Single(byTheQuery.Candidates).RelativePath);

            var byTheCalibration = await RagShortlist.BuildAsync(
                TestIndex.Build(new EmbeddingCalibration(0.95f, 0f, 5, 0, 0), null, nodes),
                Vectorizer(new[] { 1f, 0f }),
                _query,
                options,
                default
                );

            //a model whose nonsense reaches 0.95 makes even this hit unconvincing, and the measured
            //number wins over the one the query alone would have picked
            Assert.Equal(0.95f, byTheCalibration.AppliedMinScore, 4);
            Assert.Empty(byTheCalibration.Candidates);
        }

        [Fact]
        public async Task A_probe_shows_the_rows_the_per_query_threshold_cut_off()
        {
            var index = TestIndex.Build(
                TestIndex.Background(400)
                    .Append(new TestIndex.Node("A\\Answer.cs", "Answer", "the answer", TestIndex.OfScore(0.9f)))
                    .ToArray()
                );

            var probed = await RagShortlist.ProbeAsync(
                index,
                Vectorizer(new[] { 1f, 0f }),
                _query,
                new RagShortlistOptions(),
                default
                );

            //nothing cut off, and the threshold a real search would have used is reported next to
            //the scores for the calibration window to paint
            Assert.True(probed.Candidates.Count > 1);
            Assert.Equal("A\\Answer.cs", probed.Candidates[0].RelativePath);
            Assert.True(probed.Candidates[1].Score < probed.AppliedMinScore);
        }

        private static IEmbeddingVectorizer Vectorizer(
            float[] queryVector
            )
        {
            var result = new FakeVectorizer(queryVector.Length);
            result.Add(_query, queryVector);
            return result;
        }

        private static EmbeddingIndex BuildIndex(
            params (string RelativePath, string Target, string OutlineText, float[] Vector)[] nodes
            )
        {
            return BuildIndex(null, null, nodes);
        }

        private static EmbeddingIndex BuildIndex(
            EmbeddingCalibration? calibration = null,
            EmbeddingSpaceFingerprint? fingerprint = null,
            (string RelativePath, string Target, string OutlineText, float[] Vector)[]? nodes = null
            )
        {
            return TestIndex.Build(
                calibration,
                fingerprint,
                Array.ConvertAll(
                    nodes ?? Array.Empty<(string, string, string, float[])>(),
                    n => new TestIndex.Node(n.RelativePath, n.Target, n.OutlineText, n.Vector)
                    )
                );
        }
    }
}
