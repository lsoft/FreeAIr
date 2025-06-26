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
            EmbeddingContainer container
            )
        {
            if (container is null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var outlines = container.GetOutlineList();

            var embeddings = await _embeddingClient.GenerateEmbeddingsAsync(
                outlines.ConvertAll(n => n.OutlineText) //TODO split by LLM context size
                );

            for (var i = 0; i < outlines.Count; i++)
            {
                outlines[i].AddEmbedding(
                    embeddings.Value[i].ToFloats().ToArray()
                    );
            }
        }
    }
}
