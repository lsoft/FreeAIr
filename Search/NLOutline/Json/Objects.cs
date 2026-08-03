using FreeAIr.NLOutline.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.NLOutline.Json
{
    /// <summary>
    /// One node of the index as it is stored in `…_embeddings.outlines.json`. This is the whole
    /// structure of the index: the parent/child relation is not stored, it is derived back from
    /// <see cref="Kind"/> and <see cref="RelativePath"/> by <see cref="OutlineTreeAssembler"/>.
    /// </summary>
    public sealed class OutlineItselfJsonObject
    {
        public Guid Id
        {
            get;
            set;
        }

        public OutlineKindEnum Kind
        {
            get;
            set;
        }

        /// <summary>
        /// Path of the file this node belongs to, relative to the solution folder. Every node
        /// carries it, down to the last member, which is what makes a node → file lookup free.
        /// Empty for the solution node.
        /// </summary>
        public string RelativePath
        {
            get;
            set;
        }

        public string Target
        {
            get;
            set;
        }

        public string OutlineText
        {
            get;
            set;
        }

        public OutlineItselfJsonObject()
        {
            RelativePath = string.Empty;
            Target = string.Empty;
            OutlineText = string.Empty;
        }

        public OutlineItselfJsonObject(
            OutlineNode outline
            )
        {
            if (outline is null)
            {
                throw new ArgumentNullException(nameof(outline));
            }

            Id = outline.Id;
            Kind = outline.Kind;
            RelativePath = outline.RelativePath;
            Target = outline.Target;
            OutlineText = outline.OutlineText;
        }
    }

    public sealed class OutlinesItselfJsonObject
    {
        public List<OutlineItselfJsonObject> Outlines
        {
            get;
            set;
        }

        public OutlinesItselfJsonObject(
            )
        {
            Outlines = new List<OutlineItselfJsonObject>();
        }

        public OutlinesItselfJsonObject(
            OutlineNode root
            )
        {
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var nodes = new List<OutlineNode>();
            root.ApplyRecursive(
                node => nodes.Add(node)
                );

            //the file is committed, so its order must depend on the content and on nothing else -
            //not on the shape of the tree the nodes happened to be walked in
            nodes.Sort(OutlineNode.OrderComparison);

            Outlines = nodes.ConvertAll(
                node => new OutlineItselfJsonObject(node)
                );
        }

        public async Task SerializeAsync(
            string filePath,
            CancellationToken cancellationToken
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using var fs = new FileStream(filePath, FileMode.Create);

            await System.Text.Json.JsonSerializer.SerializeAsync(
                fs,
                this,
                new JsonSerializerOptions { WriteIndented = true },
                cancellationToken
                );
        }

        public static async Task<OutlinesItselfJsonObject?> DeserializeAsync(
            string filePath,
            CancellationToken cancellationToken = default
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return await System.Text.Json.JsonSerializer.DeserializeAsync<OutlinesItselfJsonObject>(
                fs,
                cancellationToken: cancellationToken
                );
        }
    }
}
