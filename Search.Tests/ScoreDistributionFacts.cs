using FreeAIr.Embedding;
using Xunit;

namespace FreeAIr.Search.Tests
{
    /// <summary>
    /// The threshold a search derives from its own scores, without a calibration and without asking
    /// the model anything: see <see cref="ScoreDistribution"/>.
    /// </summary>
    public sealed class ScoreDistributionFacts
    {
        [Fact]
        public void The_quantiles_are_read_off_the_whole_scan()
        {
            //the scan is what makes this free, so the numbers have to come from the full ranking
            //and not from the head the shortlist keeps
            var index = TestIndex.Build(TestIndex.Background(400));

            var found = index.Search(
                new[] { 1f, 0f },
                10,
                out var distribution
                );

            Assert.Equal(10, found.Count);

            Assert.NotNull(distribution);
            Assert.Equal(400, distribution!.Count);
            Assert.Equal(0.40f, distribution.Best, 3);

            //400 scores spread evenly over 0.30..0.40: the middle one sits at 0.35 and a quarter of
            //them stay below 0.325
            Assert.InRange(distribution.Median, 0.345f, 0.355f);
            Assert.InRange(distribution.LowerQuartile, 0.320f, 0.330f);
        }

        [Fact]
        public void An_empty_index_has_no_distribution()
        {
            var index = TestIndex.Build();

            index.Search(new[] { 1f, 0f }, 10, out var distribution);

            Assert.Null(distribution);
        }

        [Fact]
        public void A_sample_too_small_to_have_quantiles_gets_no_threshold()
        {
            //a quarter of a dozen numbers is not a quantile, and a made up threshold would be worse
            //than none at all
            var small = new ScoreDistribution(
                ScoreDistribution.MinimumSampleSize - 1,
                0.9f,
                0.35f,
                0.30f
                );

            Assert.Equal(0f, small.ComputeThreshold(0.2d));

            var enough = new ScoreDistribution(
                ScoreDistribution.MinimumSampleSize,
                0.9f,
                0.35f,
                0.30f
                );

            Assert.True(enough.ComputeThreshold(0.2d) > 0f);
        }

        [Fact]
        public void A_distribution_with_no_spread_gets_no_threshold()
        {
            //everything scores the same, so nothing can be told from anything by score
            var flat = new ScoreDistribution(1000, 0.5f, 0.5f, 0.5f);

            Assert.Equal(0f, flat.RobustSigma);
            Assert.Equal(0f, flat.ComputeThreshold(0.2d));
            Assert.Equal(0f, flat.BestDeviations);
        }

        [Fact]
        public void The_threshold_is_a_count_of_sigmas_above_the_median()
        {
            //median 0.35, lower quartile 0.30: that half spread is 0.6745 sigma, so sigma is 0.0741
            var distribution = new ScoreDistribution(1000, 0.9f, 0.35f, 0.30f);

            var sigma = distribution.RobustSigma;
            Assert.Equal(0.0741f, sigma, 3);

            //zero asks for two sigmas, the default 0.2 for three, one for seven
            Assert.Equal(0.35f + (2f * sigma), distribution.ComputeThreshold(0d), 4);
            Assert.Equal(0.35f + (3f * sigma), distribution.ComputeThreshold(0.2d), 4);
            Assert.Equal(0.35f + (7f * sigma), distribution.ComputeThreshold(1d), 4);

            //and the knob is clamped rather than extrapolated
            Assert.Equal(distribution.ComputeThreshold(1d), distribution.ComputeThreshold(4d), 4);
            Assert.Equal(distribution.ComputeThreshold(0d), distribution.ComputeThreshold(-1d), 4);
        }

        [Fact]
        public void The_spread_is_taken_from_the_lower_half_alone()
        {
            //the upper half is where the answers are: measuring the spread through it would widen
            //the background every time a query works, and the threshold would rise with it
            var few = new ScoreDistribution(1000, 0.55f, 0.35f, 0.30f);
            var many = new ScoreDistribution(1000, 0.95f, 0.35f, 0.30f);

            Assert.Equal(few.ComputeThreshold(0.2d), many.ComputeThreshold(0.2d), 5);
        }

        [Fact]
        public void One_setting_means_the_same_thing_on_models_of_different_scale()
        {
            //this is the whole reason the threshold is expressed in sigmas: a model which squeezes
            //its scores into a narrow band high up has to get the same verdict as one which spreads
            //them out low down
            var wide = new ScoreDistribution(1000, 0.95f, 0.30f, 0.20f);
            var narrow = new ScoreDistribution(1000, 0.95f, 0.86f, 0.84f);

            Assert.Equal(
                (wide.ComputeThreshold(0.2d) - wide.Median) / wide.RobustSigma,
                (narrow.ComputeThreshold(0.2d) - narrow.Median) / narrow.RobustSigma,
                4
                );

            //while the two thresholds themselves have nothing in common: the number which filters
            //the wide model falls below the narrow model's lower quartile, where it would let
            //three quarters of the corpus through. That is what a constant cosine cannot cope with.
            Assert.True(wide.ComputeThreshold(0.2d) < narrow.LowerQuartile);
        }

        [Fact]
        public void How_far_the_best_hit_stands_out_is_reported()
        {
            var found = new ScoreDistribution(1000, 0.90f, 0.35f, 0.30f);
            var nothing = new ScoreDistribution(1000, 0.38f, 0.35f, 0.30f);

            //a real answer is many sigmas out of the background
            Assert.True(found.BestDeviations > 7f);

            //while the best of a query this corpus cannot answer is barely off the middle
            Assert.True(nothing.BestDeviations < 1f);
            Assert.True(nothing.Best < nothing.ComputeThreshold(0.2d));
        }
    }
}
