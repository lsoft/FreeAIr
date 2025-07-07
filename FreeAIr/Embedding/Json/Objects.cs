using FreeAIr.Git;
using FreeAIr.Helper;
using FreeAIr.NLOutline.Json;
using FreeAIr.NLOutline.Tree;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
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
            OutlineNode root
            )
        {
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            OutlineTree = new OutlineTreeJsonObject(
                OutlineNodeJsonObject.Create(
                    root
                    )
                );
            Outlines = new OutlinesItselfJsonObject(
                root
                );
            Embeddings = new EmbeddingsJsonObject(
                root
                );
            GenerateDateTime = DateTime.Now;

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

            FilePath = filePath;

            var otFilePath = GetOutlineTreeFileName();
            await OutlineTree.SerializeAsync(
                otFilePath,
                cancellationToken
                );

            var oFilePath = GetOutlinesFileName();
            await Outlines.SerializeAsync(
                oFilePath,
                cancellationToken
                );

            var eFilePath = GetEmbeddingsFileName();
            await Embeddings.SerializeAsync(
                eFilePath,
                cancellationToken
                );

            using var fs = new FileStream(filePath, FileMode.Create);
            await System.Text.Json.JsonSerializer.SerializeAsync(
                fs,
                this,
                new JsonSerializerOptions { WriteIndented = true },
                cancellationToken
                );
        }

        public async Task LoadOutlinesAsync()
        {
            if (Outlines is not null)
            {
                return;
            }

            var oFilePath = GetOutlinesFileName();
            Outlines = await OutlinesItselfJsonObject.DeserializeAsync(oFilePath);
        }

        public async Task LoadOutlineTreeAsync()
        {
            if (OutlineTree is not null)
            {
                return;
            }

            var otFilePath = GetOutlineTreeFileName();
            OutlineTree = await OutlineTreeJsonObject.DeserializeAsync(otFilePath);
        }

        public async Task LoadEmbeddingsAsync()
        {
            if (Embeddings is not null)
            {
                return;
            }

            var eFilePath = GetEmbeddingsFileName();
            Embeddings = await EmbeddingsJsonObject.DeserializeAsync(eFilePath);
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


        public static async System.Threading.Tasks.Task<string?> GenerateFilePathAsync()
        {
            var solution = await VS.Solutions.GetCurrentSolutionAsync();
            if (solution is null)
            {
                return null;
            }

            var solutionFileInfo = new FileInfo(solution.Name);

            var solutionName = solution.Name;
            if (solutionFileInfo.Extension.Length > 0)
            {
                solutionName = solutionName.Substring(0, solutionName.Length - solutionFileInfo.Extension.Length);
            }

            var folderPath = System.IO.Path.Combine(
                solutionFileInfo.Directory.FullName,
                ".freeair"
                );
            if (!System.IO.Directory.Exists(folderPath))
            {
                System.IO.Directory.CreateDirectory(folderPath);
            }

            var filePath = System.IO.Path.Combine(
                folderPath,
                $"{solutionName}_embeddings.json"
                );
            return filePath;
        }

        public IReadOnlyDictionary<Guid, Triple> CreateTripleDictionary(
            )
        {
            var od = Outlines.Outlines.ToDictionary(o => o.Id, o => o);
            var otd = new Dictionary<Guid, OutlineNodeJsonObject>();
            OutlineTree.ApplyRecursive(
                o => otd[o.Id] = o
                );
            var ed = Embeddings.Embeddings.ToDictionary(e => e.Id, e => e);

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


        private string GetOutlineTreeFileName()
        {
            var fi = new FileInfo(FilePath);

            return Path.Combine(
                fi.Directory.FullName,
                fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length) + ".outlineTree" + fi.Extension
                );
        }

        private string GetOutlinesFileName()
        {
            var fi = new FileInfo(FilePath);

            return Path.Combine(
                fi.Directory.FullName,
                fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length) + ".outlines" + fi.Extension
                );
        }


        private string GetEmbeddingsFileName()
        {
            var fi = new FileInfo(FilePath);

            return Path.Combine(
                fi.Directory.FullName,
                fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length) + ".embeddings" + fi.Extension
                );
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

        public EmbeddingsJsonObject(
            OutlineNode root
            )
        {
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            Embeddings = new List<EmbeddingItselfJsonObject>();

            root.ApplyRecursive(
                node => Embeddings.Add(
                    new EmbeddingItselfJsonObject(
                        node.Id,
                        node.Embedding
                        )
                    )
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

        public static async Task<EmbeddingsJsonObject> DeserializeAsync(
            string filePath
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            using var fs = new FileStream(filePath, FileMode.Open);
            var result = await System.Text.Json.JsonSerializer.DeserializeAsync<EmbeddingsJsonObject>(fs);
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


    public sealed class Triple
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
    }
}
