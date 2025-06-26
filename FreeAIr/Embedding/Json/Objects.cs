using FreeAIr.Helper;
using FreeAIr.NLOutline.Json;
using FreeAIr.NLOutline.Tree;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FreeAIr.Embedding.Json
{
    public sealed class EmbeddingOutlineJsonObject
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public OutlineTreeJsonObject OutlineTree
        {
            get;
            set;
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public OutlinesItselfJsonObject Outlines
        {
            get;
            set;
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public EmbeddingsJsonObject Embeddings
        {
            get;
            set;
        }


        public string FilePath
        {
            get;
            set;
        }

        public DateTime GenerateDateTime
        {
            get;
            set;
        }

        public EmbeddingOutlineJsonObject()
        {
        }

        public EmbeddingOutlineJsonObject(
            OutlineTreeJsonObject outlineTree,
            OutlinesItselfJsonObject outlines,
            EmbeddingsJsonObject embeddings
            )
        {
            if (outlineTree is null)
            {
                throw new ArgumentNullException(nameof(outlineTree));
            }

            if (outlines is null)
            {
                throw new ArgumentNullException(nameof(outlines));
            }

            if (embeddings is null)
            {
                throw new ArgumentNullException(nameof(embeddings));
            }

            OutlineTree = outlineTree;
            Outlines = outlines;
            Embeddings = embeddings;
            GenerateDateTime = DateTime.Now;
        }

        public async Task SerializeAsync(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;

            var otFilePath = GetOutlineTreeFileName();
            await OutlineTree.SerializeAsync(
                otFilePath
                );

            var oFilePath = GetOutlinesFileName();
            await Outlines.SerializeAsync(
                oFilePath
                );

            var eFilePath = GetEmbeddingsFileName();
            await Embeddings.SerializeAsync(
                eFilePath
                );

            using var fs = new FileStream(filePath, FileMode.Create);
            await System.Text.Json.JsonSerializer.SerializeAsync(
                fs,
                this,
                new JsonSerializerOptions { WriteIndented = true }
                );
        }

        public async Task LoadOutlinesAsync()
        {
            if (Outlines is not null)
            {
                return;
            }

            Outlines = await OutlinesItselfJsonObject.DeserializeAsync(FilePath);
        }

        public async Task LoadOutlineTreeAsync()
        {
            if (OutlineTree is not null)
            {
                return;
            }

            OutlineTree = await OutlineTreeJsonObject.DeserializeAsync(FilePath);
        }

        public async Task LoadEmbeddingsAsync()
        {
            if (Embeddings is not null)
            {
                return;
            }

            Embeddings = await EmbeddingsJsonObject.DeserializeAsync(FilePath);
        }

        public static async Task<EmbeddingOutlineJsonObject> DeserializeAsync(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using var fs0 = new FileStream(filePath, FileMode.Open);
            var result = await System.Text.Json.JsonSerializer.DeserializeAsync<EmbeddingOutlineJsonObject>(fs0);

            return result;
        }

        public async Task DeserializeOutlineTreeAsync(
            )
        {
            this.OutlineTree = await OutlineTreeJsonObject.DeserializeAsync(
                GetOutlineTreeFileName()
                );
        }

        public void ClearOutlineTree()
        {
            this.OutlineTree = null;
        }

        public async Task DeserializeOutlinesAsync(
            )
        {
            this.Outlines = await OutlinesItselfJsonObject.DeserializeAsync(
                GetOutlinesFileName()
                );
        }

        public void ClearOutlines()
        {
            this.Outlines = null;
        }


        public async Task DeserializeEmbeddingsAsync(
            )
        {
            this.Embeddings = await EmbeddingsJsonObject.DeserializeAsync(
                GetEmbeddingsFileName()
                );
        }

        public void ClearEmbeddings()
        {
            this.Embeddings = null;
        }


        private string GetOutlineTreeFileName()
        {
            var fi = new FileInfo(FilePath);

            return Path.Combine(
                fi.Directory.FullName,
                fi.Name + ".outlineTree" + fi.Extension
                );
        }

        private string GetOutlinesFileName()
        {
            var fi = new FileInfo(FilePath);

            return Path.Combine(
                fi.Directory.FullName,
                fi.Name + ".outlines" + fi.Extension
                );
        }


        private string GetEmbeddingsFileName()
        {
            var fi = new FileInfo(FilePath);

            return Path.Combine(
                fi.Directory.FullName,
                fi.Name + ".embeddings" + fi.Extension
                );
        }

        public static EmbeddingOutlineJsonObject CreateFromScratch(
            OutlineDescriptor outline
            )
        {
            var result = new EmbeddingOutlineJsonObject
            {
                FilePath = string.Empty,
                GenerateDateTime = DateTime.Now,
                Outlines = OutlinesItselfJsonObject.CreateFromScratch(
                    outline
                    ),
                OutlineTree = OutlineTreeJsonObject.CreateFromScratch(outline),
                Embeddings = EmbeddingsJsonObject.CreateEmpty()
            };
            return result;
        }
    }

    public sealed class EmbeddingsJsonObject
    {
        public List<EmbeddingItselfJsonObject> Embeddings
        {
            get;
            set;
        }

        public EmbeddingsJsonObject(
            )
        {
        }

        public EmbeddingsJsonObject(List<EmbeddingItselfJsonObject> embeddings)
        {
            Embeddings = embeddings;
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

        public static async Task<EmbeddingsJsonObject> DeserializeAsync(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using var fs = new FileStream(filePath, FileMode.Open);
            return await System.Text.Json.JsonSerializer.DeserializeAsync<EmbeddingsJsonObject>(fs);
        }

        public static EmbeddingsJsonObject CreateEmpty()
        {
            var result = new EmbeddingsJsonObject
            {
                Embeddings = new()
            };
            return result;
        }
    }


    public sealed class EmbeddingItselfJsonObject
    {
        public Guid Id
        {
            get;
            set;
        }

        [JsonConverter(typeof(PseudoX16BlobJsonConverter))]
        public float[] Embedding
        {
            get;
            set;
        }

        public EmbeddingItselfJsonObject()
        {
        }

        public EmbeddingItselfJsonObject(Guid id, float[] embedding)
        {
            Id = id;
            Embedding = embedding;
        }
    }
}
