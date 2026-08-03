using System;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// What the index knows about its own scale of similarity, measured when it was built and
    /// stored in the metadata file.
    ///
    /// It exists because a threshold cannot be a constant. The same query against the same corpus
    /// scores 0.94 with one model, 0.84 with another and 0.70 with a third, while their noise sits
    /// at 0.89, 0.53 and 0.53 respectively — a number which filters usefully for one of them
    /// accepts everything or refuses everything for the others. So instead of shipping a number,
    /// the index is asked a few questions with a known answer and a few with no answer at all, and
    /// the threshold is derived from where those two land.
    /// </summary>
    public sealed class EmbeddingCalibration
    {
        /// <summary>
        /// The best score reached by a query which has no answer in this solution — the level at
        /// which the model is simply talking to itself.
        /// </summary>
        public float NoiseCeiling
        {
            get;
        }

        /// <summary>
        /// The worst score among the queries whose answer the user pointed at, or zero when nobody
        /// pointed at anything. A threshold must never climb up to this, or the search would start
        /// dropping answers known to be right.
        /// </summary>
        public float RelevantFloor
        {
            get;
        }

        public int IrrelevantProbeCount
        {
            get;
        }

        public int RelevantProbeCount
        {
            get;
        }

        /// <summary>
        /// How many of the labelled queries did not find their file at the top. Not used by the
        /// threshold — it is the number to show the user, because it says the model is wrong about
        /// their code rather than badly tuned.
        /// </summary>
        public int RelevantMissCount
        {
            get;
        }

        public bool HasRelevantFloor => RelevantProbeCount > 0 && RelevantFloor > 0f;

        /// <summary>
        /// Whether the model tells this codebase apart at all: the answers it got right have to
        /// score above the nonsense. When this is false no threshold helps and the model has to be
        /// replaced.
        /// </summary>
        public bool ModelSeparates => !HasRelevantFloor || RelevantFloor > NoiseCeiling;

        public EmbeddingCalibration(
            float noiseCeiling,
            float relevantFloor,
            int irrelevantProbeCount,
            int relevantProbeCount,
            int relevantMissCount
            )
        {
            NoiseCeiling = noiseCeiling;
            RelevantFloor = relevantFloor;
            IrrelevantProbeCount = irrelevantProbeCount;
            RelevantProbeCount = relevantProbeCount;
            RelevantMissCount = relevantMissCount;
        }

        /// <summary>
        /// The cosine below which a file is not worth showing.
        ///
        /// <paramref name="sensitivity"/> is how far above the noise to stand, as a share of the
        /// room left between the noise and a perfect match: 0.1 is generous, 0.2 is the default,
        /// 0.35 is strict. Expressing it this way is what makes one setting mean the same thing on
        /// models whose absolute scales have nothing in common.
        ///
        /// When the user has labelled some answers, the threshold is additionally kept below them
        /// with a quarter of the gap to spare: a knob is allowed to be strict, but not to the point
        /// of dropping a file the user has personally pointed at.
        /// </summary>
        public float ComputeThreshold(
            double sensitivity
            )
        {
            var ceiling = Clamp(NoiseCeiling, 0f, 1f);
            var share = (float)Clamp(sensitivity, 0d, 1d);

            var result = ceiling + (share * (1f - ceiling));

            if (HasRelevantFloor && RelevantFloor > ceiling)
            {
                var headroom = RelevantFloor - (0.25f * (RelevantFloor - ceiling));
                if (result > headroom)
                {
                    result = headroom;
                }
            }

            return result;
        }

        private static float Clamp(float value, float min, float max)
        {
            return value < min ? min : (value > max ? max : value);
        }

        private static double Clamp(double value, double min, double max)
        {
            return value < min ? min : (value > max ? max : value);
        }
    }
}
