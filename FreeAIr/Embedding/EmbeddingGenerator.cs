using FreeAIr.Agents;
using FreeAIr.Embedding.Json;
using FreeAIr.NLOutline.Tree;
using FreeAIr.Shared.Helper;
using OpenAI;
using OpenAI.Embeddings;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Embedding
{
    public sealed class EmbeddingGenerator
    {
        private readonly EmbeddingClient _embeddingClient;

        public EmbeddingGenerator(
            Agent agent
            )
        {
            if (agent is null)
            {
                throw new ArgumentNullException(nameof(agent));
            }

            _embeddingClient = new EmbeddingClient(
                agent.Technical.ChosenModel,
                new ApiKeyCredential(
                    agent.Technical.Token
                    ),
                new OpenAIClientOptions
                {
                    NetworkTimeout = TimeSpan.FromHours(1),
                    Endpoint = new Uri(agent.Technical.Endpoint),
                }
                );
        }

        public async Task GenerateEmbeddingsAsync(
            OutlineNode outline
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
                    outlinesIdBody.ConvertAll(n => n.Item2) //TODO split by LLM context size
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
