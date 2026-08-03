using FreeAIr.NLOutline.Json;
using FreeAIr.NLOutline.Tree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Embedding.Json
{
    /// <summary>
    /// The metadata file of the index, `&lt;solution name&gt;_embeddings.json`, plus lazy access to
    /// its two siblings.
    ///
    /// Everything stored here has to be a property of the index itself. Nothing which changes by
    /// itself between two rebuilds of the same sources goes in — no timestamps, no absolute paths,
    /// no counters — because these files are committed, and a field like that turns every
    /// regeneration into a diff and every pair of branches into a conflict.
    /// </summary>
    public sealed class EmbeddingOutlineJsonObject
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public OutlinesItselfJsonObject? Outlines
        {
            get;
            set;
        }

        [System.Text.Json.Serialization.JsonIgnore]
        public EmbeddingsJsonObject? Embeddings
        {
            get;
            set;
        }

        /// <summary>
        /// Where the file has been read from or is being written to. Deliberately NOT serialized:
        /// it is an absolute path of one particular machine.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public string? FilePath
        {
            get;
            set;
        }

        /// <summary>
        /// When the index has been built. Taken from the file system, not from the json: a
        /// timestamp inside a committed file conflicts on every regeneration, while telling
        /// nothing that the file system does not already know.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime GenerateDateTime
        {
            get;
            set;
        }

        /// <summary>
        /// The agent whose model has produced the vectors. A natural language search has to
        /// vectorize its query with exactly the same model, otherwise the query vector and the
        /// stored ones are not comparable at all.
        /// </summary>
        public string? EmbeddingAgentName
        {
            get;
            set;
        }

        public string? EmbeddingModel
        {
            get;
            set;
        }

        /// <summary>
        /// The model as the server named it in its answer, which is the only name that changes when
        /// the user loads a different model into the same local server. Written for the human to
        /// read; the machine uses <see cref="SpaceFingerprint"/>.
        /// </summary>
        public string? ReportedEmbeddingModel
        {
            get;
            set;
        }

        public string? EmbeddingEndpoint
        {
            get;
            set;
        }

        /// <summary>
        /// Vectors of the sentinel sentences, encoded like every other vector of the index. Lets a
        /// search verify that the model it holds is the one which built this file, see
        /// <see cref="EmbeddingSpaceFingerprint"/>.
        /// </summary>
        public List<string>? SpaceFingerprint
        {
            get;
            set;
        }

        /// <summary>
        /// Where the similarity of this model on this solution turned out to lie, measured at build
        /// time. Absent when the index has not been calibrated.
        /// </summary>
        public CalibrationJsonObject? Calibration
        {
            get;
            set;
        }

        /// <summary>
        /// Length of a single embedding vector. Zero when nothing has been embedded at all.
        /// </summary>
        public int EmbeddingDimensions
        {
            get;
            set;
        }

        /// <summary>
        /// How the vectors in the `.embeddings.jsonl` file are encoded, see <see cref="VectorCodec"/>.
        /// Written so that the encoding can be changed later without guessing what an existing
        /// file contains.
        /// </summary>
        public string? VectorEncoding
        {
            get;
            set;
        }

        public EmbeddingOutlineJsonObject()
        {
        }

        public EmbeddingOutlineJsonObject(
            OutlineNode root,
            string? embeddingAgentName = null,
            string? embeddingModel = null,
            string? embeddingEndpoint = null
            )
        {
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            Outlines = new OutlinesItselfJsonObject(
                root
                );
            Embeddings = new EmbeddingsJsonObject(
                root
                );

            EmbeddingAgentName = embeddingAgentName;
            EmbeddingModel = embeddingModel;
            EmbeddingEndpoint = embeddingEndpoint;

            EmbeddingDimensions = Embeddings.Embeddings
                .Find(e => e.Embedding is not null && e.Embedding.Length > 0)
                ?.Embedding
                ?.Length
                ?? 0;
            VectorEncoding = VectorCodec.Int8Base64EncodingName;
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

            var fileSet = new EmbeddingIndexFileSet(filePath);

            await Outlines!.SerializeAsync(
                fileSet.OutlinesFilePath,
                cancellationToken
                );

            await Embeddings!.SerializeAsync(
                fileSet.VectorsFilePath,
                cancellationToken
                );

            await SerializeMetadataAsync(
                filePath,
                cancellationToken
                );
        }

        /// <summary>
        /// Writes the metadata file alone and leaves its two siblings on disk untouched.
        ///
        /// This is how a recalibration is stored: the numbers of <see cref="Calibration"/> are a
        /// property of the queries they were measured with, not of the vectors, and the vectors are
        /// megabytes which cost the user an hour of a model's time. Whoever calls this has to have
        /// read the file first — everything not set here is written out as it stands.
        /// </summary>
        public async Task SerializeMetadataAsync(
            string filePath,
            CancellationToken cancellationToken
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            FilePath = filePath;

            using var fs = new FileStream(filePath, FileMode.Create);
            await System.Text.Json.JsonSerializer.SerializeAsync(
                fs,
                this,
                new JsonSerializerOptions { WriteIndented = true },
                cancellationToken
                );
        }

        /// <summary>
        /// Reads the metadata file. The siblings are left on disk unless <paramref name="full"/>
        /// is set — they are megabytes, and most callers only want to know which model built the
        /// index.
        /// </summary>
        public static async Task<EmbeddingOutlineJsonObject?> DeserializeAsync(
            string filePath,
            bool full,
            CancellationToken cancellationToken = default
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            EmbeddingOutlineJsonObject? result;

            using (var fs0 = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                result = await System.Text.Json.JsonSerializer.DeserializeAsync<EmbeddingOutlineJsonObject>(
                    fs0,
                    cancellationToken: cancellationToken
                    );
            }

            if (result is null)
            {
                return null;
            }

            //neither of these is stored in the file, see the properties themselves
            result.FilePath = filePath;
            result.GenerateDateTime = File.GetLastWriteTime(filePath);

            if (full)
            {
                await result.LoadOutlinesAsync(cancellationToken);
                await result.LoadEmbeddingsAsync(null, cancellationToken);
            }

            return result;
        }

        public async Task LoadOutlinesAsync(
            CancellationToken cancellationToken = default
            )
        {
            if (Outlines is not null)
            {
                return;
            }

            Outlines = await OutlinesItselfJsonObject.DeserializeAsync(
                new EmbeddingIndexFileSet(FilePath!).OutlinesFilePath,
                cancellationToken
                );
        }

        public async Task LoadEmbeddingsAsync(
            IProgress<long>? bytesRead = null,
            CancellationToken cancellationToken = default
            )
        {
            if (Embeddings is not null)
            {
                return;
            }

            Embeddings = await EmbeddingsJsonObject.DeserializeAsync(
                new EmbeddingIndexFileSet(FilePath!).VectorsFilePath,
                bytesRead,
                cancellationToken
                );
        }

        public void ClearOutlines()
        {
            this.Outlines = null;
        }

        public void ClearEmbeddings()
        {
            this.Embeddings = null;
        }

        /// <summary>
        /// The outline tree this file describes. The shape is derived rather than stored, see
        /// <see cref="OutlineTreeAssembler"/>. Requires the outlines to be loaded; the vectors are
        /// attached to the nodes when they are loaded too.
        /// </summary>
        public OutlineNode? BuildOutlineTree(
            )
        {
            if (Outlines is null)
            {
                throw new InvalidOperationException(
                    "The outlines are not loaded, call " + nameof(LoadOutlinesAsync) + " first."
                    );
            }

            Dictionary<Guid, float[]>? vectors = null;

            if (Embeddings is not null)
            {
                vectors = new Dictionary<Guid, float[]>();
                foreach (var embedding in Embeddings.Embeddings)
                {
                    if (embedding.Embedding is null || embedding.Embedding.Length == 0)
                    {
                        continue;
                    }

                    vectors[embedding.Id] = embedding.Embedding;
                }
            }

            return OutlineTreeAssembler.Assemble(
                Outlines.Outlines,
                vectors
                );
        }
    }

    /// <summary>
    /// The calibration numbers as they are stored, see <see cref="EmbeddingCalibration"/>. A plain
    /// bag of properties: the type which interprets them lives outside the json layer.
    /// </summary>
    public sealed class CalibrationJsonObject
    {
        public float NoiseCeiling
        {
            get;
            set;
        }

        public float RelevantFloor
        {
            get;
            set;
        }

        public int IrrelevantProbeCount
        {
            get;
            set;
        }

        public int RelevantProbeCount
        {
            get;
            set;
        }

        public int RelevantMissCount
        {
            get;
            set;
        }

        public CalibrationJsonObject()
        {
        }

        public CalibrationJsonObject(
            EmbeddingCalibration calibration
            )
        {
            if (calibration is null)
            {
                throw new ArgumentNullException(nameof(calibration));
            }

            NoiseCeiling = calibration.NoiseCeiling;
            RelevantFloor = calibration.RelevantFloor;
            IrrelevantProbeCount = calibration.IrrelevantProbeCount;
            RelevantProbeCount = calibration.RelevantProbeCount;
            RelevantMissCount = calibration.RelevantMissCount;
        }

        public EmbeddingCalibration ToCalibration()
        {
            return new EmbeddingCalibration(
                NoiseCeiling,
                RelevantFloor,
                IrrelevantProbeCount,
                RelevantProbeCount,
                RelevantMissCount
                );
        }
    }

    /// <summary>
    /// The vectors of the index, stored in `…_embeddings.embeddings.jsonl` as one json object per
    /// line.
    /// </summary>
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
            Embeddings = new List<EmbeddingItselfJsonObject>();
        }

        public EmbeddingsJsonObject(
            OutlineNode root
            )
        {
            if (root is null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            var embedded = new List<OutlineNode>();

            root.ApplyRecursive(
                node =>
                {
                    //solution, project and file nodes have no outline text of their own, and
                    //nodes whose outline is just their own identifier are not embedded either
                    if (node.Embedding is null || node.Embedding.Length == 0)
                    {
                        return;
                    }

                    embedded.Add(node);
                }
                );

            //the order of the file has to depend on its contents only, see the comparison itself
            embedded.Sort(OutlineNode.OrderComparison);

            Embeddings = embedded.ConvertAll(
                node => new EmbeddingItselfJsonObject(
                    node.Id,
                    node.Embedding!
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

            using var writer = new StreamWriter(filePath, false);

            foreach (var embedding in Embeddings)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var encoded = VectorCodec.Encode(embedding.Embedding);
                if (encoded.Length == 0)
                {
                    continue;
                }

                var line = System.Text.Json.JsonSerializer.Serialize(
                    new EmbeddingLineJsonObject
                    {
                        Id = embedding.Id,
                        V = encoded
                    }
                    );

                await writer.WriteLineAsync(line);
            }
        }

        /// <summary>
        /// Reads the vectors back, already L2 normalized, so that a cosine similarity between any
        /// two of them is a plain dot product. <paramref name="bytesRead"/> is reported as the
        /// file is consumed and is meant to drive a progress bar — on a real solution this file is
        /// megabytes long.
        /// </summary>
        public static async Task<EmbeddingsJsonObject> DeserializeAsync(
            string filePath,
            IProgress<long>? bytesRead = null,
            CancellationToken cancellationToken = default
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var result = new EmbeddingsJsonObject();

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(fs);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync();
                if (line is null)
                {
                    break;
                }

                bytesRead?.Report(fs.Position);

                if (line.Length == 0)
                {
                    continue;
                }

                var deserialized = System.Text.Json.JsonSerializer.Deserialize<EmbeddingLineJsonObject>(line);
                if (deserialized is null)
                {
                    continue;
                }

                var vector = VectorCodec.DecodeNormalized(deserialized.V);
                if (vector is null)
                {
                    continue;
                }

                result.Embeddings.Add(
                    new EmbeddingItselfJsonObject(
                        deserialized.Id,
                        vector
                        )
                    );
            }

            return result;
        }
    }

    /// <summary>
    /// A single line of the `.embeddings.jsonl` file. The property names are short on purpose:
    /// they are repeated on every one of the thousands of lines.
    /// </summary>
    public sealed class EmbeddingLineJsonObject
    {
        public Guid Id
        {
            get;
            set;
        }

        /// <summary>The vector, see <see cref="VectorCodec"/>.</summary>
        public string? V
        {
            get;
            set;
        }
    }


    public sealed class EmbeddingItselfJsonObject
    {
        public Guid Id
        {
            get;
            set;
        }

        public float[] Embedding
        {
            get;
            set;
        }

        public EmbeddingItselfJsonObject()
        {
            Embedding = Array.Empty<float>();
        }

        public EmbeddingItselfJsonObject(Guid id, float[] embedding)
        {
            Id = id;
            Embedding = embedding;
        }
    }
}
