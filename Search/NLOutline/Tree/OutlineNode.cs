using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

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

        /// <summary>
        /// The order the children of any node are kept in, and the order the index files are
        /// written in.
        ///
        /// It has to be a total order: all the members of one file share a <see cref="RelativePath"/>,
        /// and <see cref="List{T}.Sort(Comparison{T})"/> is not stable, so ordering by the path
        /// alone would let them come out differently from run to run. The files built out of this
        /// tree are meant to be committed, where that shows up as a diff nobody has made and as a
        /// conflict for anyone merging two branches.
        /// </summary>
        public static readonly Comparison<OutlineNode> OrderComparison =
            (a, b) =>
            {
                var r = string.CompareOrdinal(a.RelativePath, b.RelativePath);
                if (r != 0)
                {
                    return r;
                }

                r = string.CompareOrdinal(a.Target, b.Target);
                if (r != 0)
                {
                    return r;
                }

                return a.Id.CompareTo(b.Id);
            };

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

        /// <summary>
        /// The identity of a node: it links the two index files, and it is what tells a rebuilt
        /// node from a new one. Derived from the content rather than generated, so that rebuilding
        /// the index on another machine produces the very same ids.
        /// </summary>
        public static Guid GenerateGuid(
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
                new List<OutlineNode>()
                );
            _children.Add(result);

            SortChildren();

            return result;
        }

        public void SortChildren()
        {
            _children.Sort(OrderComparison);
        }

        public T? ApplyRecursive<T>(
            Func<OutlineNode, T?> action
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

        /// <summary>
        /// Walks the tree until <paramref name="action"/> returns false, which stops the walk.
        /// </summary>
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

            return true;
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
    }
}
