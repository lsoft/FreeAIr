using FreeAIr.NLOutline.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace FreeAIr.NLOutline.Json
{
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

        public string FullPath
        {
            get;
            set;
        }

        public string OutlineText
        {
            get;
            set;
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public Guid EmbeddingId => Id;

        public OutlineItselfJsonObject(
            OutlineDescriptor outline
            )
        {
            Id = outline.Id;
            Kind = outline.Kind;
            FullPath = outline.FullPath;
            OutlineText = outline.OutlineText;
        }
    }

    public sealed class OutlineNodeJsonObject
    {
        public Guid Id
        {
            get;
            set;
        }

        public List<OutlineNodeJsonObject> Children
        {
            get;
            set;
        }

        public OutlineNodeJsonObject(
            OutlineDescriptor outline
            )
        {
            Id = outline.Id;
            Children = new();
        }

        public void ApplyRecursive(
            Action<OutlineNodeJsonObject> action
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
        }

        public OutlinesItselfJsonObject(List<OutlineItselfJsonObject> outlines)
        {
            Outlines = outlines;
        }

        public async Task SerializeAsync(
            string filePath
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
                new JsonSerializerOptions { WriteIndented = true }
                );
        }

        public static async Task<OutlinesItselfJsonObject> DeserializeAsync(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using var fs = new FileStream(filePath, FileMode.Open);
            return await System.Text.Json.JsonSerializer.DeserializeAsync<OutlinesItselfJsonObject>(fs);
        }

        public static OutlinesItselfJsonObject CreateFromScratch(
            OutlineDescriptor outline
            )
        {
            var result = new OutlinesItselfJsonObject
            {
                Outlines =
                [
                    new OutlineItselfJsonObject(
                        outline
                        )
                ]
            };
            return result;
        }
    }

    public sealed class OutlineTreeJsonObject
    {
        public OutlineNodeJsonObject Root
        {
            get;
            set;
        }

        public OutlineTreeJsonObject()
        {
        }

        public OutlineTreeJsonObject(OutlineNodeJsonObject root)
        {
            Root = root;
        }

        public async Task SerializeAsync(
            string filePath
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
                new JsonSerializerOptions { WriteIndented = true }
                );
        }

        public static async Task<OutlineTreeJsonObject> DeserializeAsync(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using var fs = new FileStream(filePath, FileMode.Open);
            return await System.Text.Json.JsonSerializer.DeserializeAsync<OutlineTreeJsonObject>(fs);
        }

        public static OutlineTreeJsonObject CreateFromScratch(
            OutlineDescriptor outline
            )
        {
            var result = new OutlineTreeJsonObject
            {
                Root = new OutlineNodeJsonObject(outline)
            };

            return result;
        }

        public void ApplyRecursive(
            Action<OutlineNodeJsonObject> action
            )
        {
            if (action is null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            Root.ApplyRecursive(action);
        }
    }
}
