using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Find
{
    /// <summary>
    /// A question whose answer the user has pointed at. The threshold is not allowed to climb above
    /// the score this one gets, see <see cref="EmbeddingCalibration.RelevantFloor"/>.
    /// </summary>
    public sealed class RagRelevantProbe
    {
        public string Query
        {
            get;
        }

        /// <summary>The file which is supposed to come out on top, relative to the solution.</summary>
        public string ExpectedRelativePath
        {
            get;
        }

        public RagRelevantProbe(
            string query,
            string expectedRelativePath
            )
        {
            Query = query;
            ExpectedRelativePath = expectedRelativePath;
        }
    }

    /// <summary>
    /// The questions the index is calibrated with.
    ///
    /// The interesting half is the negative one, and it is the half the user has to write: the
    /// built-in nonsense below is far enough from any code to score low on every model, which makes
    /// it a safe default and a weak one. A query which is plausible for a codebase yet absent from
    /// this one — "how is oauth configured" in a project which has no authentication — scores much
    /// higher, and it is that number which the threshold has to clear. Only the author of the
    /// solution knows such questions.
    /// </summary>
    public sealed class RagCalibrationProbes
    {
        /// <summary>
        /// Questions no source tree can answer. Two languages, because a model may be fluent in one
        /// of them only, and the noise level of the weaker language is the one that matters.
        /// </summary>
        public static readonly IReadOnlyList<string> DefaultIrrelevant = new[]
        {
            "recipe for a mushroom soup with sour cream",
            "рецепт грибного супа со сметаной",
            "what is the income tax rate in norway",
            "прогноз погоды на завтра в москве",
            "how to plant tomatoes in a greenhouse",
        };

        public IReadOnlyList<RagRelevantProbe> Relevant
        {
            get;
        }

        public IReadOnlyList<string> Irrelevant
        {
            get;
        }

        public bool IsEmpty => Relevant.Count == 0 && Irrelevant.Count == 0;

        public RagCalibrationProbes(
            IReadOnlyList<RagRelevantProbe>? relevant = null,
            IReadOnlyList<string>? irrelevant = null
            )
        {
            Relevant = relevant ?? Array.Empty<RagRelevantProbe>();
            Irrelevant = irrelevant is null || irrelevant.Count == 0
                ? DefaultIrrelevant
                : irrelevant
                ;
        }

        public static RagCalibrationProbes Default => new RagCalibrationProbes();
    }

    /// <summary>
    /// Measures where the similarity of a freshly built index lies, so that the search can have a
    /// threshold which means the same thing on any model. Runs once per index build; the cost is
    /// one batch of a few short strings.
    /// </summary>
    public static class RagCalibrator
    {
        /// <summary>
        /// Scores the probes against the index. Returns null when there is nothing to measure — an
        /// empty index, or a model which cannot answer — and the index is then stored without
        /// calibration, which the search reads as "apply no threshold".
        /// </summary>
        public static async Task<EmbeddingCalibration?> RunAsync(
            EmbeddingIndex index,
            IEmbeddingVectorizer vectorizer,
            RagCalibrationProbes probes,
            CancellationToken cancellationToken
            )
        {
            if (index is null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (vectorizer is null)
            {
                throw new ArgumentNullException(nameof(vectorizer));
            }

            if (probes is null)
            {
                throw new ArgumentNullException(nameof(probes));
            }

            if (index.Entries.Count == 0)
            {
                return null;
            }

            var texts = new List<string>(probes.Irrelevant.Count + probes.Relevant.Count);
            foreach (var irrelevant in probes.Irrelevant)
            {
                texts.Add(irrelevant);
            }

            foreach (var relevant in probes.Relevant)
            {
                texts.Add(relevant.Query);
            }

            if (texts.Count == 0)
            {
                return null;
            }

            var vectors = await vectorizer.VectorizeAsync(
                texts,
                cancellationToken
                );

            if (vectors is null || vectors.Count != texts.Count)
            {
                throw new InvalidOperationException(
                    $"The vectorizer was asked for {texts.Count} vectors and answered with {vectors?.Count ?? 0}."
                    );
            }

            var noiseCeiling = 0f;

            for (var i = 0; i < probes.Irrelevant.Count; i++)
            {
                var best = BestScore(index, vectors[i], cancellationToken);
                if (best > noiseCeiling)
                {
                    noiseCeiling = best;
                }
            }

            var relevantFloor = 0f;
            var misses = 0;
            var hits = 0;

            for (var i = 0; i < probes.Relevant.Count; i++)
            {
                var probe = probes.Relevant[i];
                var vector = vectors[probes.Irrelevant.Count + i];

                var score = ScoreOf(index, vector, probe.ExpectedRelativePath, cancellationToken);
                if (score is null)
                {
                    //the file the user pointed at is not in the index, or scored below everything
                    //the search would ever look at
                    misses++;
                    continue;
                }

                hits++;

                if (relevantFloor <= 0f || score.Value < relevantFloor)
                {
                    relevantFloor = score.Value;
                }
            }

            return new EmbeddingCalibration(
                noiseCeiling,
                relevantFloor,
                probes.Irrelevant.Count,
                hits + misses,
                misses
                );
        }

        /// <summary>
        /// Calibrates an index which is still in memory, on its way to disk, and writes both the
        /// numbers and the fingerprint into the metadata it is about to be serialized from.
        /// </summary>
        public static async Task ApplyToAsync(
            EmbeddingOutlineJsonObject json,
            IEmbeddingVectorizer vectorizer,
            RagCalibrationProbes probes,
            CancellationToken cancellationToken
            )
        {
            if (json is null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (vectorizer is null)
            {
                throw new ArgumentNullException(nameof(vectorizer));
            }

            if (json.Outlines is null || json.Embeddings is null)
            {
                throw new InvalidOperationException(
                    "The index has to carry both its outlines and its vectors to be calibrated."
                    );
            }

            var fingerprint = await EmbeddingSpaceFingerprint.BuildAsync(
                vectorizer,
                cancellationToken
                );
            json.SpaceFingerprint = fingerprint?.Encode();

            //asked after the first request, because that is when the server has had a chance to
            //name the model it actually holds
            json.ReportedEmbeddingModel = vectorizer.ReportedModelName;

            var index = EmbeddingIndex.Build(
                new EmbeddingIndexMetadata(
                    json.FilePath ?? string.Empty,
                    json.GenerateDateTime,
                    json.EmbeddingAgentName,
                    json.EmbeddingModel,
                    json.EmbeddingDimensions
                    ),
                json.Outlines.Outlines,
                json.Embeddings.Embeddings,
                cancellationToken
                );

            var calibration = await RunAsync(
                index,
                vectorizer,
                probes,
                cancellationToken
                );

            json.Calibration = calibration is null
                ? null
                : new CalibrationJsonObject(calibration)
                ;
        }

        private static float BestScore(
            EmbeddingIndex index,
            float[] vector,
            CancellationToken cancellationToken
            )
        {
            var found = index.Search(vector, 1, cancellationToken);
            return found.Count == 0
                ? 0f
                : found[0].Score
                ;
        }

        /// <summary>
        /// What the expected file scores for this query, folded the way the shortlist folds it: the
        /// best outline of the file. Null when the file is nowhere near the top, which is a miss
        /// rather than a low score — the model is wrong about this codebase, and a threshold cannot
        /// fix that.
        /// </summary>
        private static float? ScoreOf(
            EmbeddingIndex index,
            float[] vector,
            string expectedRelativePath,
            CancellationToken cancellationToken
            )
        {
            if (string.IsNullOrEmpty(expectedRelativePath))
            {
                return null;
            }

            //the same head the search itself ranks: a file which is not in it would never be
            //shown, so its score is not a floor worth protecting
            var found = index.Search(vector, _relevantSearchDepth, cancellationToken);

            foreach (var pair in found)
            {
                if (string.Equals(pair.Entry.RelativePath, expectedRelativePath, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Score;
                }
            }

            return null;
        }

        /// <summary>
        /// How deep a labelled answer is looked for. Matches the default depth of the shortlist:
        /// counting a file which the search would never reach as a hit would drag the floor down to
        /// a score nobody ever sees.
        /// </summary>
        private const int _relevantSearchDepth = 50;
    }
}
