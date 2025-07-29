using FreeAIr.Embedding.Json;
using FreeAIr.NLOutline.Json;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.NLOutline.Tree
{
    public enum OutlineKindEnum
    {
        Solution = 1,
        Project = 2,
        File = 3,
        ClassOrSimilarEntity = 4,
        MethodOfClassOrSimilarPart = 5
    }


    public sealed class OutlineNode
    {
        private readonly List<OutlineNode> _children;

        public Guid Id
        {
            get;
        }

        public OutlineKindEnum Kind
        {
            get;
        }

        public string RelativePath
        {
            get;
        }

        public string Target
        {
            get;
        }

        public string OutlineText
        {
            get;
        }

        public float[]? Embedding
        {
            get;
            private set;
        }

        public IReadOnlyList<OutlineNode> Children => _children;

        public OutlineNode(
            Guid id,
            OutlineKindEnum kind,
            string relativePath,
            string target,
            string outlineText,
            float[]? embedding,
            List<OutlineNode> children
            )
        {
            Id = id;
            Kind = kind;
            RelativePath = relativePath;
            Target = target;
            OutlineText = outlineText;
            Embedding = embedding;
            _children = children;
        }

        public OutlineNode(
            OutlineKindEnum kind,
            string relativePath,
            string target,
            string outlineText,
            float[]? embedding,
            List<OutlineNode> children
            ) : this(
                GenerateGuid(kind, target, relativePath),
                kind,
                relativePath,
                target,
                outlineText,
                embedding,
                children
                )
        {
        }

        private static Guid GenerateGuid(
            OutlineKindEnum kind,
            string target,
            string? relativePath
            )
        {
            var s = kind.ToString() + ":" + target + ":" + relativePath;

            if (string.IsNullOrEmpty(s))
            {
                return Guid.Empty;
            }

            var byt = Encoding.UTF8.GetBytes(s);
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(byt);
            var guid = new Guid(hash);

            return guid;
        }

        public void AddChild(
            OutlineNode child
            )
        {
            if (child is null)
            {
                throw new ArgumentNullException(nameof(child));
            }

            _children.Add(child);

            SortChildren();
        }

        public OutlineNode AddChild(
            OutlineKindEnum kind,
            string relativePath,
            string target,
            string outlineText
            )
        {
            var result = new OutlineNode(
                kind,
                relativePath,
                target,
                outlineText,
                null,
                []
                );
            _children.Add(result);

            SortChildren();

            return result;
        }

        public static async Task<OutlineNode?> CreateAsync(
            string jsonEmbeddingFilePath,
            bool full
            )
        {
            if (jsonEmbeddingFilePath is null)
            {
                throw new ArgumentNullException(nameof(jsonEmbeddingFilePath));
            }

            var json = await EmbeddingOutlineJsonObject.DeserializeAsync(
                jsonEmbeddingFilePath,
                full
                );
            var outlineRoot = OutlineNode.Create(json);
            return outlineRoot;
        }

        public static OutlineNode? Create(
            EmbeddingOutlineJsonObject root
            )
        {
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var tripleDictionary = root.CreateTripleDictionary();

            var result = Create(
                tripleDictionary,
                root.OutlineTree.Root
                );
            return result;
        }

        private void SortChildren()
        {
            _children.Sort((a, b) => a.RelativePath.CompareTo(b.RelativePath));
        }

        public T ApplyRecursive<T>(
            Func<OutlineNode, T> action
            ) where T : class
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var r = action(this);
            if (r is not null)
            {
                return r;
            }

            foreach (var child in Children)
            {
                r = child.ApplyRecursive(action);
                if (r is not null)
                {
                    return r;
                }
            }

            return null;
        }

        public bool ApplyRecursive(
            Func<OutlineNode, bool> action
            )
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var r = action(this);
            if (!r)
            {
                return false;
            }

            foreach (var child in Children)
            {
                r = child.ApplyRecursive(action);
                if (!r)
                {
                    return false;
                }
            }

            return false;
        }

        public void ApplyRecursive(
            Action<OutlineNode> action
            )
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action(this);

            foreach (var child in Children)
            {
                child.ApplyRecursive(action);
            }
        }

        public void AddEmbedding(float[] embedding)
        {
            if (embedding is null)
            {
                throw new ArgumentNullException(nameof(embedding));
            }

            if (Embedding is not null)
            {
                throw new InvalidOperationException("Embedding already set");
            }

            Embedding = embedding;
        }

        private static OutlineNode? Create(
            IReadOnlyDictionary<Guid, Triple> tripleDictionary,
            OutlineNodeJsonObject root
            )
        {
            if (tripleDictionary is null)
            {
                throw new ArgumentNullException(nameof(tripleDictionary));
            }

            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (!tripleDictionary.TryGetValue(root.Id, out var triple))
            {
                return null;
            }

            var node = new OutlineNode(
                root.Id,
                triple.OutlineItself.Kind,
                triple.OutlineItself.FullPath,
                triple.OutlineItself.Target,
                triple.OutlineItself.OutlineText,
                triple.EmbeddingItself.Embedding,
                root.Children
                    .ConvertAll(c => Create(tripleDictionary, c))
                    .FindAll(c => c is not null)
                );

            return node;
        }

    }
}
