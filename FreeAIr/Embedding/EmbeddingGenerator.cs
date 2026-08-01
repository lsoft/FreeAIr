using FreeAIr.Options2.Agent;
using FreeAIr.NLOutline.Tree;
using OpenAI;
using OpenAI.Embeddings;
using System.ClientModel;
using System.Collections.Generic;
using System.Threading;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// Turns the text of natural language outlines into embedding vectors, which are then stored
    /// in `.freeair\&lt;solution name&gt;_embeddings.json` and used to narrow down a natural language
    /// search.
    ///
    /// The agent given to the constructor must point at an embedding model, not at a chat one.
    /// </summary>
    public sealed class EmbeddingGenerator
    {
        private readonly EmbeddingClient _embeddingClient;

        public EmbeddingGenerator(
            AgentJson agent
            )
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            _embeddingClient = new EmbeddingClient(
                agent.Technical.ChosenModel,
                new ApiKeyCredential(
                    agent.Technical.GetToken()
                    ),
                new OpenAIClientOptions
                {
                    NetworkTimeout = TimeSpan.FromHours(1),
                    Endpoint = new Uri(agent.Technical.Endpoint),
                }
                );
        }

        /// <summary>
        /// Walks the outline tree and fills in the embeddings that are still missing, in a single
        /// batched request. Nodes which already have an embedding are left alone, so re-running
        /// this after adding a few outlines is cheap.
        /// </summary>
        public async Task GenerateEmbeddingsAsync(
            OutlineNode outline,
            CancellationToken cancellationToken
            )
        {
            if (outline is null)
            {
                throw new ArgumentNullException(nameof(outline));
            }

            var outlinesIdBody = new List<(OutlineNode, string)>();

            outline.ApplyRecursive(
                node =>
                {
                    if (node.Embedding is null)
                    {
                        outlinesIdBody.Add((node, node.OutlineText));
                    }
                }
                );

            if (outlinesIdBody.Count > 0)
            {
                var embeddings = await _embeddingClient.GenerateEmbeddingsAsync(
                    outlinesIdBody.ConvertAll(n => n.Item2), //TODO split by LLM context size
                    cancellationToken: cancellationToken
                    );

                for (var i = 0; i < outlinesIdBody.Count; i++)
                {
                    outlinesIdBody[i].Item1.AddEmbedding(
                        embeddings.Value[i].ToFloats().ToArray()
                        );
                }
            }
        }
    }
}
