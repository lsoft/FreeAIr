using FreeAIr.Embedding;
using Xunit;

namespace FreeAIr.Search.Tests
{
    public sealed class VectorCodecFacts
    {
        [Fact]
        public void A_decoded_vector_points_where_the_original_did()
        {
            var random = new Random(20260802);

            for (var attempt = 0; attempt < 200; attempt++)
            {
                var original = RandomVector(random, 1536);

                var decoded = VectorCodec.DecodeNormalized(
                    VectorCodec.Encode(original)
                    );

                Assert.NotNull(decoded);

                var normalizedOriginal = (float[])original.Clone();
                Assert.True(VectorCodec.NormalizeInPlace(normalizedOriginal));

                var cosine = VectorCodec.DotProduct(normalizedOriginal, decoded!);

                //this is the price of int8 quantization, and the whole reason the index shrank
                //six times. The gap between a meaningful score and a meaningless one is two orders
                //of magnitude larger than this.
                Assert.True(
                    cosine > 0.994f,
                    $"cosine similarity of the round trip dropped to {cosine}"
                    );
            }
        }

        [Fact]
        public void A_decoded_vector_is_of_unit_length()
        {
            var decoded = VectorCodec.DecodeNormalized(
                VectorCodec.Encode(new[] { 3f, 0f, 4f })
                );

            Assert.NotNull(decoded);
            Assert.Equal(1d, VectorCodec.DotProduct(decoded!, decoded!), 5);
        }

        [Fact]
        public void The_scale_of_a_vector_does_not_survive_and_does_not_matter()
        {
            //the quantization scale is deliberately not stored: everything is normalized before use
            var small = VectorCodec.DecodeNormalized(VectorCodec.Encode(new[] { 1f, 2f, 3f }))!;
            var large = VectorCodec.DecodeNormalized(VectorCodec.Encode(new[] { 1000f, 2000f, 3000f }))!;

            Assert.Equal(1d, VectorCodec.DotProduct(small, large), 4);
        }

        [Fact]
        public void A_vector_with_no_direction_is_refused_rather_than_stored()
        {
            Assert.Equal(string.Empty, VectorCodec.Encode(new[] { 0f, 0f, 0f }));
            Assert.Equal(string.Empty, VectorCodec.Encode(Array.Empty<float>()));
            Assert.False(VectorCodec.NormalizeInPlace(new[] { 0f, 0f }));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not base64 at all !!!")]
        public void A_payload_which_is_not_a_vector_decodes_to_nothing(
            string? payload
            )
        {
            Assert.Null(VectorCodec.DecodeNormalized(payload));
        }

        [Fact]
        public void Encoding_is_stable_across_calls()
        {
            //the vectors file is committed; an encoder which produced two spellings of one vector
            //would show up as a diff after every rebuild
            var vector = new[] { 0.1f, -0.7f, 0.33f, 0.9f };

            Assert.Equal(
                VectorCodec.Encode(vector),
                VectorCodec.Encode((float[])vector.Clone())
                );
        }

        [Fact]
        public void The_dot_product_ignores_the_tail_of_the_longer_vector()
        {
            //the unrolled loop has to agree with the plain one on every length, not only on
            //multiples of four
            for (var length = 1; length <= 11; length++)
            {
                var left = new float[length];
                var right = new float[length];

                for (var i = 0; i < length; i++)
                {
                    left[i] = i + 1;
                    right[i] = (i % 3) - 1;
                }

                var expected = 0f;
                for (var i = 0; i < length; i++)
                {
                    expected += left[i] * right[i];
                }

                Assert.Equal(expected, VectorCodec.DotProduct(left, right), 3);
            }
        }

        private static float[] RandomVector(
            Random random,
            int dimensions
            )
        {
            var result = new float[dimensions];
            for (var i = 0; i < dimensions; i++)
            {
                result[i] = (float)(random.NextDouble() * 2d - 1d);
            }

            return result;
        }
    }
}
