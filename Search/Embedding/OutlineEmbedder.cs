using FreeAIr.NLOutline.Tree;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// Fills the outline tree with vectors before it is written to disk.
    /// </summary>
    public sealed class OutlineEmbedder
    {
        private readonly IEmbeddingVectorizer _vectorizer;

        public OutlineEmbedder(
            IEmbeddingVectorizer vectorizer
            )
        {
            _vectorizer = vectorizer ?? throw new ArgumentNullException(nameof(vectorizer));
        }

        /// <summary>
        /// Which nodes are worth a vector. Kept apart from the request itself because this is the
        /// decision which shapes the whole index: it is what an index of a real solution mostly
        /// consists of, and getting it wrong is invisible until a search returns nothing.
        ///
        /// Skipped are:
        /// <list type="bullet">
        /// <item>nodes which already have a vector — rerunning after adding a few outlines is cheap;</item>
        /// <item>nodes with no outline text: solution, project and (for the Roslyn scanner) file
        /// nodes carry none. An empty string in the batch is dangerous as well — a provider which
        /// silently drops empty inputs would shift every following vector onto a wrong node;</item>
        /// <item>nodes whose outline is nothing but their own identifier. No comment was found and
        /// the scanner fell back to the name; on a real solution these are the majority of the
        /// tree, and they carry nothing a plain text search would not find.</item>
        /// </list>
        /// </summary>
        public static List<OutlineNode> SelectNodesToEmbed(
            OutlineNode root
            )
        {
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var result = new List<OutlineNode>();

            root.ApplyRecursive(
                node =>
                {
                    if (node.Embedding is not null)
                    {
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(node.OutlineText))
                    {
                        return;
                    }

                    if (string.Equals(
                        node.OutlineText.Trim(),
                        node.Target?.Trim(),
                        StringComparison.Ordinal
                        ))
                    {
                        return;
                    }

                    result.Add(node);
                }
                );

            return result;
        }

        /// <summary>
        /// Walks the outline tree and fills in the embeddings that are still missing, in a single
        /// batched request.
        /// </summary>
        public async Task GenerateEmbeddingsAsync(
            OutlineNode root,
            CancellationToken cancellationToken
            )
        {
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var nodes = SelectNodesToEmbed(root);
            if (nodes.Count == 0)
            {
                return;
            }

            var vectors = await _vectorizer.VectorizeAsync(
                nodes.ConvertAll(n => n.OutlineText),
                cancellationToken
                );

            if (vectors is null || vectors.Count != nodes.Count)
            {
                //the vectors are matched to the nodes by position and there is no other way to tell
                //which is which. A short answer means every vector after the gap belongs to another
                //node, and an index built out of that is silently wrong for good.
                throw new InvalidOperationException(
                    $"The vectorizer returned {vectors?.Count ?? 0} vectors for {nodes.Count} outlines."
                    );
            }

            for (var i = 0; i < nodes.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                nodes[i].AddEmbedding(vectors[i]);
            }
        }
    }
}
