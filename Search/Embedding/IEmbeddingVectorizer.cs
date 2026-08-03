using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// The only part of the index pipeline which talks to a server.
    ///
    /// It is an interface so that everything around it can be exercised without one: a test hands
    /// over vectors it has made up and checks the ranking, while an integration test hands over
    /// the real client and checks that the ranking still means something.
    /// </summary>
    public interface IEmbeddingVectorizer
    {
        /// <summary>The model that was asked for, i.e. what the settings say.</summary>
        string ModelName
        {
            get;
        }

        /// <summary>
        /// The model the server itself named in its last answer, or null before the first one.
        ///
        /// It is worth asking about because <see cref="ModelName"/> is whatever the user typed into
        /// the settings, and a local server is normally configured with a placeholder: koboldcpp,
        /// for one, lists its only model as `inactive` and names the real one solely in the body of
        /// the reply. This is a label for the human, not a guard — that is
        /// <see cref="EmbeddingSpaceFingerprint"/>.
        /// </summary>
        string? ReportedModelName
        {
            get;
        }

        /// <summary>
        /// Vectorizes a batch. The result must have exactly one vector per input, in the same
        /// order: the caller matches them back to its nodes by position and has no other way to
        /// tell which is which.
        /// </summary>
        Task<IReadOnlyList<float[]>> VectorizeAsync(
            IReadOnlyList<string> texts,
            CancellationToken cancellationToken
            );
    }

    public static class EmbeddingVectorizerExtensions
    {
        /// <summary>
        /// Vectorizes a single piece of text — a natural language search query. The result is
        /// comparable with the stored outline vectors only when the same model produced both.
        /// </summary>
        public static async Task<float[]> VectorizeOneAsync(
            this IEmbeddingVectorizer vectorizer,
            string text,
            CancellationToken cancellationToken
            )
        {
            if (vectorizer is null)
            {
                throw new ArgumentNullException(nameof(vectorizer));
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException($"'{nameof(text)}' cannot be null or whitespace.", nameof(text));
            }

            var result = await vectorizer.VectorizeAsync(
                new[] { text },
                cancellationToken
                );

            if (result is null || result.Count == 0)
            {
                throw new InvalidOperationException("The vectorizer returned nothing for a non empty text.");
            }

            return result[0];
        }
    }
}
