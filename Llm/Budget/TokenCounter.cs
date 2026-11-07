using System.Collections.Concurrent;

namespace FreeAIr.Llm.Budget
{
    /// <summary>
    /// Counts the tokens of a request for one model as closely as can be done without its
    /// tokenizer, and says how many of them fit into the model's context window.
    ///
    /// The count is the <see cref="TokenEstimator"/> guess multiplied by a ratio learned from the
    /// server: after every answer the `usage` the server reported for the request is divided by
    /// the guess for that very request. Scaled that way the previous request counts as exactly
    /// what the server said, and the next one - which is the previous one plus an answer and a new
    /// prompt - is off only by the error of the guess for what was added. When the server reports
    /// nothing, a zero or an absurd number, the ratio is left as it was and the guess by
    /// characters is what counts.
    /// </summary>
    public sealed class TokenCounter
    {
        /// <summary>
        /// The bounds of a believable ratio of reported to guessed tokens. The guess is generous,
        /// so a real tokenizer lands somewhat below one; an old tokenizer on Cyrillic text goes
        /// to about two. Anything outside is a server counting something else - only the uncached
        /// part of the prompt, say - and calibrating on it would overflow the next request.
        /// </summary>
        public const double MinPlausibleRatio = 0.4;

        /// <summary>The upper bound of a believable ratio of reported to guessed tokens.</summary>
        public const double MaxPlausibleRatio = 3.0;

        /// <summary>
        /// The share of the window held back for the error of the count: small once the count is
        /// calibrated, larger while it is a guess by characters.
        /// </summary>
        public const double CalibratedMargin = 0.05;

        /// <summary>The share of the window held back while the count is still a guess by characters.</summary>
        public const double UncalibratedMargin = 0.15;

        /// <summary>
        /// The most held back for the answer when the settings name no output limit: a quarter of
        /// the window, but no more than the 8192 the Anthropic transport asks for in that case.
        /// </summary>
        public const int DefaultOutputReserveCap = 8192;

        /// <summary>The last ratio learned per model, which a new chat with that model starts from.</summary>
        private static readonly ConcurrentDictionary<string, double> _lastRatios = new(StringComparer.Ordinal);

        /// <summary>The ratio is learned on the reader's thread and read when the next request is built.</summary>
        private readonly object _locker = new();
        private double? _ratio;

        /// <summary>
        /// The learned ratio of reported to guessed tokens, or null while nothing believable has
        /// been reported yet and the guess by characters stands alone.
        /// </summary>
        public double? Ratio
        {
            get
            {
                lock (_locker)
                {
                    return _ratio;
                }
            }
        }

        /// <summary>Whether a server count has been learned, which narrows the safety margin.</summary>
        public bool IsCalibrated => Ratio.HasValue;

        /// <summary>
        /// A counter starting from a ratio learned earlier - from a saved chat, or from another
        /// chat with the same model. An implausible one is ignored rather than trusted.
        /// </summary>
        public TokenCounter(
            double? ratio = null
            )
        {
            _ratio = IsPlausible(ratio) ? ratio : null;
        }

        /// <summary>
        /// The ratio the last calibrated counter of this model learned, so that a new chat does
        /// not start from the bare guess. Null when no chat has talked to the model yet.
        /// </summary>
        public static double? GetLastRatio(
            string modelKey
            )
        {
            if (modelKey is null)
            {
                throw new ArgumentNullException(nameof(modelKey));
            }

            return _lastRatios.TryGetValue(modelKey, out var ratio)
                ? ratio
                : null;
        }

        /// <summary>Whether a ratio is one a real tokenizer could have produced against the guess.</summary>
        public static bool IsPlausible(
            double? ratio
            )
        {
            return ratio.HasValue
                && !double.IsNaN(ratio.Value)
                && ratio.Value >= MinPlausibleRatio
                && ratio.Value <= MaxPlausibleRatio
                ;
        }

        /// <summary>A raw <see cref="TokenEstimator"/> guess converted to this model's tokens.</summary>
        public int Scale(
            int rawEstimate
            )
        {
            var ratio = Ratio;
            if (!ratio.HasValue)
            {
                return rawEstimate;
            }

            return (int)Math.Ceiling(rawEstimate * ratio.Value);
        }

        /// <summary>
        /// Learns from what the server reported for a request whose raw guess was
        /// <paramref name="rawEstimate"/>. Returns false, keeping the old ratio, when the count
        /// is missing, zero, or so far from the guess that it cannot be a count of this request.
        /// </summary>
        public bool TryCalibrate(
            string modelKey,
            int rawEstimate,
            int? reportedInputTokens
            )
        {
            if (modelKey is null)
            {
                throw new ArgumentNullException(nameof(modelKey));
            }

            if (rawEstimate <= 0 || !reportedInputTokens.HasValue || reportedInputTokens.Value <= 0)
            {
                return false;
            }

            var ratio = (double)reportedInputTokens.Value / rawEstimate;
            if (!IsPlausible(ratio))
            {
                LlmDiagnostics.Report(
                    $"The server reported {reportedInputTokens.Value} input tokens for model '{modelKey}' where about {rawEstimate} were expected. "
                    + "The count is ignored and the context budget keeps its previous calibration."
                    );
                return false;
            }

            lock (_locker)
            {
                _ratio = ratio;
            }

            _lastRatios[modelKey] = ratio;

            return true;
        }

        /// <summary>
        /// How many tokens of request fit into a window of <paramref name="contextSize"/>: what is
        /// left after the answer's share and the safety margin. A window of zero or less means the
        /// size is unknown, and then nothing is ever cut.
        /// </summary>
        public int GetInputBudget(
            int contextSize,
            int? maxOutputTokens
            )
        {
            if (contextSize <= 0)
            {
                return int.MaxValue;
            }

            var outputReserve = maxOutputTokens.HasValue && maxOutputTokens.Value > 0
                ? maxOutputTokens.Value
                : Math.Min(contextSize / 4, DefaultOutputReserveCap);

            var available = contextSize - outputReserve;
            if (available <= 0)
            {
                return 0;
            }

            var margin = IsCalibrated ? CalibratedMargin : UncalibratedMargin;

            return (int)(available * (1.0 - margin));
        }
    }
}
