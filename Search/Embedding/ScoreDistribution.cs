using System;
using System.Collections.Generic;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// Where the scores of one query landed across the whole index — the shape of the similarity
    /// distribution, not its top.
    ///
    /// It exists because the search can measure its own noise for free. A search is a linear scan
    /// (see <see cref="EmbeddingIndex.Search"/>), so every entry has already been scored by the time
    /// the head is cut off, and in an index of a few thousand outlines a handful of them are
    /// relevant while everything else is background. That background is exactly what a threshold
    /// has to stand above, measured on this query, with this model, against this corpus — no probe
    /// queries, no settings and no requests to the server.
    ///
    /// This is what <see cref="EmbeddingCalibration"/> cannot do: a calibration is one number for
    /// the whole index, while a short generic query is close to everything in the corpus and a
    /// narrow one is close to nothing. The two are used together, see
    /// <see cref="FreeAIr.Find.RagShortlist"/>.
    /// </summary>
    public sealed class ScoreDistribution
    {
        /// <summary>
        /// How many scored entries are needed before the quartiles below mean anything. A quarter
        /// of a dozen numbers is not a quantile, so a small index — a toy solution, or a unit test —
        /// gets no threshold from here at all rather than a made up one.
        /// </summary>
        public const int MinimumSampleSize = 256;

        /// <summary>
        /// Turns the lower half of the distribution into a standard deviation. The half distance
        /// between the median and the lower quartile is 0.6745 sigma for a normal distribution, and
        /// dividing it out puts <see cref="RobustSigma"/> on the same scale as a plain sigma.
        /// </summary>
        private const float _quartileToSigma = 0.6745f;

        /// <summary>How many sigmas above the median the threshold sits at sensitivity zero.</summary>
        private const float _baseDeviations = 2f;

        /// <summary>How many more sigmas a full unit of sensitivity buys, see <see cref="ComputeThreshold"/>.</summary>
        private const float _deviationsPerShare = 5f;

        /// <summary>How many entries of the index were scored, i.e. the size of the sample.</summary>
        public int Count
        {
            get;
        }

        /// <summary>The best score of the query — the top of the ranking the shortlist is cut from.</summary>
        public float Best
        {
            get;
        }

        /// <summary>
        /// The middle of the distribution: the level this query sits at against an arbitrary piece
        /// of the corpus. A generic query pulls this up and a narrow one pushes it down, which is
        /// the whole point of measuring it per query.
        /// </summary>
        public float Median
        {
            get;
        }

        /// <summary>
        /// The score a quarter of the entries stay below. Taken from the lower half on purpose: the
        /// upper half is where the relevant answers live, and a spread measured through them would
        /// grow with the number of files which actually match — the threshold would then rise
        /// exactly when the query works.
        /// </summary>
        public float LowerQuartile
        {
            get;
        }

        /// <summary>
        /// The spread of the background, in the units of a standard deviation. Zero when the lower
        /// half of the distribution is flat, which happens on a degenerate index and means nothing
        /// can be told apart by score.
        /// </summary>
        public float RobustSigma
        {
            get
            {
                var spread = Median - LowerQuartile;
                return spread <= 0f
                    ? 0f
                    : spread / _quartileToSigma
                    ;
            }
        }

        /// <summary>
        /// How far the best hit stands out of the background, in sigmas. The number to show the
        /// user next to a result: below two the query has found nothing in particular, above five
        /// it has found something the corpus really does contain.
        /// </summary>
        public float BestDeviations
        {
            get
            {
                var sigma = RobustSigma;
                return sigma <= 0f
                    ? 0f
                    : (Best - Median) / sigma
                    ;
            }
        }

        public ScoreDistribution(
            int count,
            float best,
            float median,
            float lowerQuartile
            )
        {
            Count = count;
            Best = best;
            Median = median;
            LowerQuartile = lowerQuartile;
        }

        /// <summary>
        /// The cosine below which a file is indistinguishable from the background of this query.
        ///
        /// <paramref name="sensitivity"/> is the same knob the calibration reads, mapped to a
        /// distance from the median measured in <see cref="RobustSigma"/>: the default 0.2 asks for
        /// three sigmas, 0.1 for two and a half, 0.35 for just under four. Sigmas are what makes one
        /// setting mean the same thing on models whose absolute scales have nothing in common — the
        /// same reason the calibration expresses itself as a share rather than as a cosine.
        ///
        /// Returns zero — meaning "no threshold" — for a sample too small to have quantiles and for
        /// a distribution with no spread at all.
        /// </summary>
        public float ComputeThreshold(
            double sensitivity
            )
        {
            if (Count < MinimumSampleSize)
            {
                return 0f;
            }

            var sigma = RobustSigma;
            if (sigma <= 0f)
            {
                return 0f;
            }

            var share = (float)(sensitivity < 0d ? 0d : (sensitivity > 1d ? 1d : sensitivity));
            var deviations = _baseDeviations + (_deviationsPerShare * share);

            return Median + (deviations * sigma);
        }

        /// <summary>
        /// Reads the distribution off the scored entries of one search. The list has to be the
        /// whole scan sorted best first — which is what <see cref="EmbeddingIndex.Search"/> has in
        /// hand before it cuts the head off, so this costs three array reads and no arithmetic.
        /// </summary>
        internal static ScoreDistribution? Measure(
            List<(EmbeddingIndexEntry Entry, float Score)> sortedByScoreDescending
            )
        {
            if (sortedByScoreDescending is null)
            {
                throw new ArgumentNullException(nameof(sortedByScoreDescending));
            }

            var count = sortedByScoreDescending.Count;
            if (count == 0)
            {
                return null;
            }

            //nearest rank, counted from the top because that is how the list is ordered: the median
            //has half the sample above it, the lower quartile three quarters
            var median = sortedByScoreDescending[count / 2].Score;
            var lowerQuartile = sortedByScoreDescending[(count * 3) / 4].Score;

            return new ScoreDistribution(
                count,
                sortedByScoreDescending[0].Score,
                median,
                lowerQuartile
                );
        }
    }
}
