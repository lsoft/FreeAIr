using FreeAIr.Embedding.Json;
using FreeAIr.NLOutline.Json;
using FreeAIr.NLOutline.Tree;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FreeAIr.Embedding
{
    public sealed class EmbeddingContainer
    {
        private readonly EmbeddingOutlineJsonObject _jsonObject;
        private readonly Dictionary<Guid, Triple> _triples;


        public EmbeddingContainer(
            EmbeddingOutlineJsonObject jsonObject
            )
        {
            if (jsonObject is null)
            {
                throw new ArgumentNullException(nameof(jsonObject));
            }

            _jsonObject = jsonObject;

            _triples = FillTriples(jsonObject);
        }

        public OutlineTree GetRootOutlineNode()
        {
            return new OutlineTree(
                this,
                _jsonObject.OutlineTree.Root.Id
                );
        }

        public List<OutlineBody> GetOutlineList()
        {
            return _jsonObject.Outlines.Outlines.ConvertAll(
                o => new OutlineBody(this, o.Id, o.OutlineText)
                );
        }

        public void AddEmbedding(
            Guid outlineId,
            float[] embedding
            )
        {
            if (!_triples.TryGetValue(outlineId, out var triple))
            {
                return;
            }

            var embeddingItself = new EmbeddingItselfJsonObject(outlineId, embedding);

            triple.AddEmbedding(embeddingItself);
            _jsonObject.Embeddings.Embeddings.Add(embeddingItself);
        }

        public void AddOutline(
            Guid parentOutlineId,
            OutlineDescriptor outline
            )
        {
            if (!_triples.TryGetValue(parentOutlineId, out var parentTriple))
            {
                return;
            }
            if (_triples.ContainsKey(outline.Id))
            {
                return;
            }

            var outlineItself = new OutlineItselfJsonObject(
                outline
                );
            var outlineNode = new OutlineNodeJsonObject(
                outline
                );

            _jsonObject.Outlines.Outlines.Add(outlineItself);
            parentTriple.OutlineNode.Children.Add(outlineNode);

            var triple = new Triple(
                outlineItself,
                outlineNode,
                null
                );
            _triples[outline.Id] = triple;
        }

        public async Task LoadOutlinesAsync()
        {
            await _jsonObject.LoadOutlinesAsync();
        }

        public async Task LoadOutlineTreeAsync()
        {
            await _jsonObject.LoadOutlineTreeAsync();
        }

        public async Task LoadEmbeddingsAsync()
        {
            await _jsonObject.LoadEmbeddingsAsync();
        }

        public async Task SerializeAsync(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            await _jsonObject.SerializeAsync(filePath);
        }

        public static EmbeddingContainer CreateFromScratch(
            OutlineDescriptor outline
            )
        {
            var jsonObject = EmbeddingOutlineJsonObject.CreateFromScratch(
                outline
                );

            return new EmbeddingContainer(
                jsonObject
                );
        }

        public static async Task<EmbeddingContainer> DeserializeAsync(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var jsonObject = await EmbeddingOutlineJsonObject.DeserializeAsync(filePath);

            return new EmbeddingContainer(
                jsonObject
                );
        }

        private static Dictionary<Guid, Triple> FillTriples(
            EmbeddingOutlineJsonObject jsonObject
            )
        {
            var od = jsonObject.Outlines.Outlines.ToDictionary(o => o.Id, o => o);
            var otd = new Dictionary<Guid, OutlineNodeJsonObject>();
            jsonObject.OutlineTree.ApplyRecursive(
                o => otd[o.Id] = o
                );
            var ed = jsonObject.Embeddings.Embeddings.ToDictionary(e => e.Id, e => e);

            var triples = new Dictionary<Guid, Triple>();
            foreach (var odi in od)
            {
                if (!otd.TryGetValue(odi.Key, out var otdi))
                {
                    continue;
                }

                ed.TryGetValue(odi.Key, out var edi);

                triples[odi.Key] = new Triple(
                    odi.Value,
                    otdi,
                    edi
                    );
            }

            return triples;
        }


        private sealed class Triple
        {
            public OutlineItselfJsonObject OutlineItself
            {
                get;
            }

            public OutlineNodeJsonObject OutlineNode
            {
                get;
            }

            public EmbeddingItselfJsonObject? EmbeddingItself
            {
                get;
                private set;
            }

            public Triple(
                OutlineItselfJsonObject outlineItself,
                OutlineNodeJsonObject outlineNode,
                EmbeddingItselfJsonObject? embeddingItself
                )
            {
                if (outlineItself is null)
                {
                    throw new ArgumentNullException(nameof(outlineItself));
                }

                if (outlineNode is null)
                {
                    throw new ArgumentNullException(nameof(outlineNode));
                }


                OutlineItself = outlineItself;
                OutlineNode = outlineNode;
                EmbeddingItself = embeddingItself;
            }

            public void AddEmbedding(EmbeddingItselfJsonObject embeddingItself)
            {
                if (embeddingItself is null)
                {
                    throw new ArgumentNullException(nameof(embeddingItself));
                }

                EmbeddingItself = embeddingItself;
            }
        }

    }

    public sealed class OutlineBody
    {
        private readonly EmbeddingContainer _container;

        public Guid Id
        {
            get;
        }

        public string OutlineText
        {
            get;
        }

        public OutlineBody(
            EmbeddingContainer container,
            Guid id,
            string outlineText
            )
        {
            _container = container;
            Id = id;
            OutlineText = outlineText;
        }

        public void AddEmbedding(
            float[] embedding
            )
        {
            if (embedding is null)
            {
                throw new ArgumentNullException(nameof(embedding));
            }

            _container.AddEmbedding(Id, embedding);
        }
    }

    public sealed class OutlineTree
    {
        private readonly EmbeddingContainer _container;
        private readonly Guid _rootOutlineId;

        public OutlineTree(
            EmbeddingContainer container,
            Guid rootOutlineId
            )
        {
            if (container is null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            _container = container;
            _rootOutlineId = rootOutlineId;
        }

        public OutlineTree AddChild(
            OutlineKindEnum kind,
            string filePath,
            string outlineText
            )
        {
            var outline = new OutlineDescriptor(
                kind,
                filePath,
                outlineText
                );

            _container.AddOutline(
                _rootOutlineId,
                outline
                );

            var result = new OutlineTree(
                _container,
                outline.Id
                );
            return result;
        }
    }
}
