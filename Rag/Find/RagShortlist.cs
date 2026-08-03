using FreeAIr.Embedding;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Find
{
    /// <summary>
    /// The knobs of the shortlist. A plain object rather than the settings file itself, so that the
    /// ranking can be exercised without one.
    /// </summary>
    public sealed class RagShortlistOptions
    {
        /// <summary>How many outlines are ranked before they are folded into files.</summary>
        public int TopOutlineCount
        {
            get;
            set;
        } = 50;

        /// <summary>The most files the search is allowed to be narrowed down to.</summary>
        public int MaxFileCount
        {
            get;
            set;
        } = 15;

        /// <summary>
        /// How far above the noise of this index a file has to stand to be shown, as a share of the
        /// room between that noise and a perfect match: 0.1 is generous, 0.2 is the default, 0.35 is
        /// strict. See <see cref="EmbeddingCalibration.ComputeThreshold"/> for why the setting is
        /// not a cosine — a cosine means something different on every model, this does not.
        ///
        /// It has no effect on an index which has not been calibrated: there is nothing to measure
        /// the share against, and inventing a number would only bring back the constant it replaced.
        /// </summary>
        public double Sensitivity
        {
            get;
            set;
        } = 0.2d;
    }

    /// <summary>
    /// One file the shortlist has picked, with the reason it has been picked.
    /// </summary>
    public sealed class RagCandidate
    {
        /// <summary>Path of the file relative to the solution, as stored in the index.</summary>
        public string RelativePath
        {
            get;
        }

        /// <summary>Cosine similarity of the best outline of this file against the query.</summary>
        public float Score
        {
            get;
        }

        /// <summary>The member or type whose outline scored best. Shown to the user as an explanation.</summary>
        public string BestTarget
        {
            get;
        }

        public string BestOutlineText
        {
            get;
        }

        /// <summary>How many outlines of this file made it into the ranked head.</summary>
        public int MatchedOutlineCount
        {
            get;
            internal set;
        }

        internal RagCandidate(
            string relativePath,
            float score,
            string bestTarget,
            string bestOutlineText
            )
        {
            RelativePath = relativePath;
            Score = score;
            BestTarget = bestTarget;
            BestOutlineText = bestOutlineText;
            MatchedOutlineCount = 1;
        }
    }

    public sealed class RagShortlistResult
    {
        /// <summary>The files to search in, best first. Empty when nothing passed the threshold.</summary>
        public IReadOnlyList<RagCandidate> Candidates
        {
            get;
        }

        /// <summary>How many outlines the index holds, i.e. the size of the scan behind the result.</summary>
        public int IndexedOutlineCount
        {
            get;
        }

        public int QueryDimensions
        {
            get;
        }

        public int IndexDimensions
        {
            get;
        }

        /// <summary>
        /// The cosine a file had to reach to be listed, derived from the calibration of the index.
        /// Zero when the index carries none, i.e. when nothing has been cut off by score.
        ///
        /// In a probe (see <see cref="RagShortlist.ProbeAsync"/>) nothing has been cut off at all
        /// and this is the threshold an ordinary search would have used, which is exactly what the
        /// calibration window has to show next to the scores.
        /// </summary>
        public float AppliedMinScore
        {
            get;
        }

        /// <summary>
        /// How much the current model agrees with the one which built the index, measured on the
        /// sentinel sentences. Null when the index carries no fingerprint and the question could
        /// not be asked.
        /// </summary>
        public float? SpaceSimilarity
        {
            get;
        }

        /// <summary>
        /// The query has been vectorized by a model whose vectors are of another length than the
        /// stored ones, so the two are not comparable and nothing can match. Happens when the index
        /// carries no agent name and the user picks the wrong agent by hand.
        /// </summary>
        public bool ModelMismatch => IndexDimensions > 0
            && QueryDimensions > 0
            && IndexDimensions != QueryDimensions
            ;

        /// <summary>
        /// The vectors come from a different model than the stored ones, which the length alone
        /// does not show: two unrelated models of the same size produce an index that reads
        /// perfectly and matches nothing meaningful. Nothing has been searched in this case.
        /// </summary>
        public bool SpaceMismatch => SpaceSimilarity.HasValue
            && SpaceSimilarity.Value < EmbeddingSpaceFingerprint.SameSpaceThreshold
            ;

        internal RagShortlistResult(
            IReadOnlyList<RagCandidate> candidates,
            int indexedOutlineCount,
            int queryDimensions,
            int indexDimensions,
            float appliedMinScore = 0f,
            float? spaceSimilarity = null
            )
        {
            Candidates = candidates;
            IndexedOutlineCount = indexedOutlineCount;
            QueryDimensions = queryDimensions;
            IndexDimensions = indexDimensions;
            AppliedMinScore = appliedMinScore;
            SpaceSimilarity = spaceSimilarity;
        }
    }

    /// <summary>
    /// Narrows a natural language search down to a handful of files, using the embedding index
    /// instead of asking the LLM about every file of the solution.
    ///
    /// The shortlist is a filter, not an answer: whatever it returns is still handed to the LLM,
    /// which rechecks it. That is why the thresholds may be generous — a file taken in vain costs
    /// one more request, while a file dropped by mistake is lost for good.
    /// </summary>
    public static class RagShortlist
    {
        public static Task<RagShortlistResult> BuildAsync(
            EmbeddingIndex index,
            IEmbeddingVectorizer vectorizer,
            string query,
            RagShortlistOptions options,
            CancellationToken cancellationToken
            )
        {
            return RunAsync(
                index,
                vectorizer,
                query,
                options,
                true,
                cancellationToken
                );
        }

        /// <summary>
        /// The same ranking as <see cref="BuildAsync"/> with nothing dropped by score, for the
        /// calibration window: the whole point of that window is to look at the files which did
        /// <i>not</i> pass and to tell whether the threshold is where it belongs. The threshold a
        /// real search would apply is reported in
        /// <see cref="RagShortlistResult.AppliedMinScore"/> instead of being enforced.
        ///
        /// A model which did not build this index is still refused, exactly as in a search: those
        /// numbers would mean nothing and calibrating on them would be worse than not calibrating.
        /// </summary>
        public static Task<RagShortlistResult> ProbeAsync(
            EmbeddingIndex index,
            IEmbeddingVectorizer vectorizer,
            string query,
            RagShortlistOptions options,
            CancellationToken cancellationToken
            )
        {
            return RunAsync(
                index,
                vectorizer,
                query,
                options,
                false,
                cancellationToken
                );
        }

        private static async Task<RagShortlistResult> RunAsync(
            EmbeddingIndex index,
            IEmbeddingVectorizer vectorizer,
            string query,
            RagShortlistOptions options,
            bool applyThreshold,
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

            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException($"'{nameof(query)}' cannot be null or whitespace.", nameof(query));
            }

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            //the sentinels ride along in the request the query goes out in, so verifying the model
            //costs no round trip at all — only a few short strings in a batch which is being sent
            //anyway
            var texts = new List<string> { query };
            if (index.Fingerprint is not null)
            {
                foreach (var sentinel in EmbeddingSpaceFingerprint.SentinelTexts)
                {
                    texts.Add(sentinel);
                }
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

            cancellationToken.ThrowIfCancellationRequested();

            var queryVector = vectors[0];

            float? spaceSimilarity = null;
            if (index.Fingerprint is not null)
            {
                var sentinelVectors = new List<float[]>(texts.Count - 1);
                for (var i = 1; i < vectors.Count; i++)
                {
                    sentinelVectors.Add(vectors[i]);
                }

                spaceSimilarity = index.Fingerprint.SimilarityTo(sentinelVectors);

                if (spaceSimilarity.Value < EmbeddingSpaceFingerprint.SameSpaceThreshold)
                {
                    //scoring one model's query against another model's vectors produces numbers,
                    //and every one of them is meaningless
                    return new RagShortlistResult(
                        Array.Empty<RagCandidate>(),
                        index.Entries.Count,
                        queryVector.Length,
                        index.Dimensions,
                        0f,
                        spaceSimilarity
                        );
                }
            }

            var minScore = index.Calibration?.ComputeThreshold(options.Sensitivity) ?? 0f;

            var found = index.Search(
                queryVector,
                Math.Max(1, options.TopOutlineCount),
                cancellationToken
                );

            var candidates = Aggregate(
                found,
                options,
                applyThreshold ? minScore : float.NegativeInfinity
                );

            return new RagShortlistResult(
                candidates,
                index.Entries.Count,
                queryVector.Length,
                index.Dimensions,
                minScore,
                spaceSimilarity
                );
        }

        /// <summary>
        /// Which of <paramref name="relativePaths"/> the index knows nothing about. Such files are
        /// dropped by a RAG search simply because they were never indexed, which the user has to be
        /// told about — otherwise the search silently ignores everything added since the index was
        /// built.
        /// </summary>
        public static List<string> FindUncovered(
            EmbeddingIndex index,
            IEnumerable<string> relativePaths
            )
        {
            if (index is null)
            {
                throw new ArgumentNullException(nameof(index));
            }

            if (relativePaths is null)
            {
                throw new ArgumentNullException(nameof(relativePaths));
            }

            var result = new List<string>();

            foreach (var relativePath in relativePaths)
            {
                if (!index.IsCovered(relativePath))
                {
                    result.Add(relativePath);
                }
            }

            return result;
        }

        /// <summary>
        /// Ranked outlines to ranked files. The score of a file is the score of its best outline
        /// rather than a sum over them: summing rewards long files for having many members, which
        /// is not what the user asked about.
        /// </summary>
        internal static List<RagCandidate> Aggregate(
            List<(EmbeddingIndexEntry Entry, float Score)> found,
            RagShortlistOptions options,
            float minScore
            )
        {
            var maxFileCount = Math.Max(1, options.MaxFileCount);

            var byPath = new Dictionary<string, RagCandidate>(StringComparer.OrdinalIgnoreCase);
            var order = new List<RagCandidate>();

            foreach (var pair in found)
            {
                if (pair.Score < minScore)
                {
                    //Search returns the entries best first, so nothing below this one can pass
                    break;
                }

                if (string.IsNullOrEmpty(pair.Entry.RelativePath))
                {
                    //solution and project nodes have no file of their own; they carry no outline
                    //text either, so they are not supposed to be in the index at all
                    continue;
                }

                if (byPath.TryGetValue(pair.Entry.RelativePath, out var existing))
                {
                    existing.MatchedOutlineCount++;
                    continue;
                }

                //the first hit of a file is its best one, again because of the order of `found`
                var candidate = new RagCandidate(
                    pair.Entry.RelativePath,
                    pair.Score,
                    pair.Entry.Target,
                    pair.Entry.OutlineText
                    );

                byPath.Add(pair.Entry.RelativePath, candidate);
                order.Add(candidate);
            }

            if (order.Count > maxFileCount)
            {
                order.RemoveRange(maxFileCount, order.Count - maxFileCount);
            }

            return order;
        }
    }
}
