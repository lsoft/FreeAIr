using FreeAIr.Embedding;
using Xunit;
using Xunit.Abstractions;

namespace FreeAIr.Search.Tests
{
    /// <summary>
    /// The first contact with a real embedding server: is it there, does it answer, and are its
    /// answers usable as vectors at all.
    ///
    /// These do not build an index and do not search — they only establish that the seam works,
    /// so that a later failure of a bigger integration test can be read as a failure of our code
    /// rather than of the connection. They skip themselves when
    /// <see cref="IntegrationEnvironment.EndpointVariableName"/> is not set.
    /// </summary>
    public sealed class EmbeddingIntegrationFacts
    {
        private readonly ITestOutputHelper _output;

        public EmbeddingIntegrationFacts(
            ITestOutputHelper output
            )
        {
            _output = output;
        }

        [IntegrationFact]
        public async Task The_server_is_there_and_says_which_models_it_has()
        {
            using var timeout = CreateTimeout();

            var models = await IntegrationEnvironment.ListModelsAsync(timeout.Token);

            _output.WriteLine($"{IntegrationEnvironment.RequiredEndpoint} lists {models.Count} model(s):");
            foreach (var model in models)
            {
                _output.WriteLine("  " + model);
            }

            Assert.NotEmpty(models);
        }

        [IntegrationFact]
        public async Task The_model_turns_a_text_into_a_vector()
        {
            using var timeout = CreateTimeout();

            var vectorizer = await CreateVectorizerAsync(timeout.Token);

            var vector = await vectorizer.VectorizeOneAsync(
                "Reads and writes the embedding index files.",
                timeout.Token
                );

            _output.WriteLine($"model '{vectorizer.ModelName}' answered with {vector.Length} dimensions");

            Assert.NotEmpty(vector);
            Assert.All(vector, component => Assert.True(float.IsFinite(component), "the vector contains a NaN or an infinity"));

            //an all-zero vector encodes to nothing and would silently drop out of the index
            Assert.True(
                VectorCodec.NormalizeInPlace((float[])vector.Clone()),
                "the model answered with a vector of length zero, which cannot be stored or ranked"
                );
        }

        [IntegrationFact]
        public async Task A_batch_comes_back_in_the_order_it_was_sent()
        {
            using var timeout = CreateTimeout();

            var vectorizer = await CreateVectorizerAsync(timeout.Token);

            var texts = new[]
            {
                "Parses the JSON settings of the extension.",
                "Draws the chat window and its markdown blocks.",
                "Records the microphone and transcribes what it heard.",
            };

            var batch = await vectorizer.VectorizeAsync(texts, timeout.Token);

            //the index matches vectors back to nodes by position and has no other way to tell them
            //apart, so a provider which reorders or drops one corrupts the whole file silently
            Assert.Equal(texts.Length, batch.Count);
            Assert.All(batch, vector => Assert.Equal(batch[0].Length, vector.Length));

            for (var i = 0; i < texts.Length; i++)
            {
                var alone = await vectorizer.VectorizeOneAsync(texts[i], timeout.Token);

                var similarity = Cosine(batch[i], alone);
                _output.WriteLine($"text #{i} batched vs alone: {similarity:F4}");

                Assert.True(
                    similarity > 0.99f,
                    $"vector #{i} of the batch is not the vector of text #{i} (similarity {similarity:F4})"
                    );
            }
        }

        [IntegrationFact]
        public async Task Related_texts_score_higher_than_unrelated_ones()
        {
            using var timeout = CreateTimeout();

            var vectorizer = await CreateVectorizerAsync(timeout.Token);

            var vectors = await vectorizer.VectorizeAsync(
                new[]
                {
                    "where are the embeddings of the solution stored",
                    "Writes the index of the outlines and their vectors to disk.",
                    "Boils the mushrooms for the soup and adds sour cream.",
                },
                timeout.Token
                );

            Assert.Equal(3, vectors.Count);

            var related = Cosine(vectors[0], vectors[1]);
            var unrelated = Cosine(vectors[0], vectors[2]);

            _output.WriteLine($"related: {related:F4}, unrelated: {unrelated:F4}");

            //this is the whole premise of the feature. A model which fails it is loaded, answering
            //and useless for the search — the case that is worth catching before building an index
            //with it.
            Assert.True(
                related > unrelated,
                $"the model ranks an unrelated text ({unrelated:F4}) at least as high as a related one ({related:F4})"
                );
        }

        private static async Task<IEmbeddingVectorizer> CreateVectorizerAsync(
            CancellationToken cancellationToken
            )
        {
            return new OpenAIEmbeddingVectorizer(
                await IntegrationEnvironment.ResolveModelAsync(cancellationToken),
                IntegrationEnvironment.RequiredEndpoint,
                IntegrationEnvironment.Token
                );
        }

        /// <summary>
        /// Compares two raw model vectors the way the index does after storing them: normalized,
        /// then a dot product.
        /// </summary>
        private static float Cosine(
            float[] left,
            float[] right
            )
        {
            var a = (float[])left.Clone();
            var b = (float[])right.Clone();

            Assert.True(VectorCodec.NormalizeInPlace(a));
            Assert.True(VectorCodec.NormalizeInPlace(b));

            return VectorCodec.DotProduct(a, b);
        }

        private static CancellationTokenSource CreateTimeout()
        {
            //a local model on a cold cache is slow; a hung one should still fail the test rather
            //than the run
            return new CancellationTokenSource(TimeSpan.FromMinutes(3));
        }
    }
}
