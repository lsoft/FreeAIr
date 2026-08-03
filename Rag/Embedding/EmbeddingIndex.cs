using FreeAIr.NLOutline.Json;
using FreeAIr.NLOutline.Tree;
using System;
using System.Collections.Generic;
using System.Threading;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// One searchable node: an outline joined with its vector. Nodes without a vector do not become
    /// entries — see <see cref="EmbeddingIndex.CoveredRelativePaths"/> for the files they cover.
    /// </summary>
    public sealed class EmbeddingIndexEntry
    {
        public Guid Id
        {
            get;
        }

        public OutlineKindEnum Kind
        {
            get;
        }

        /// <summary>
        /// Path of the file this node belongs to, relative to the solution. Every node carries it,
        /// including members, so a node maps to a file without walking the outline tree.
        /// </summary>
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

        /// <summary>L2 normalized, so a cosine similarity against it is a plain dot product.</summary>
        public float[] Vector
        {
            get;
        }

        public EmbeddingIndexEntry(
            Guid id,
            OutlineKindEnum kind,
            string relativePath,
            string target,
            string outlineText,
            float[] vector
            )
        {
            Id = id;
            Kind = kind;
            RelativePath = relativePath;
            Target = target;
            OutlineText = outlineText;
            Vector = vector;
        }
    }

    /// <summary>
    /// An immutable snapshot of `.freeair\&lt;solution&gt;_embeddings.*` prepared for searching.
    ///
    /// Built by <see cref="EmbeddingIndexReader"/> — do not mutate anything reachable from it: one
    /// instance is shared by every search until the files on disk change.
    /// </summary>
    public sealed class EmbeddingIndex
    {
        /// <summary>
        /// How often the linear scan looks at the cancellation token. Checking on every entry costs
        /// more than the dot product it guards; a few thousand entries is well under a millisecond,
        /// so this still reacts instantly from the user's point of view.
        /// </summary>
        private const int _cancellationCheckInterval = 4096;

        private readonly List<EmbeddingIndexEntry> _entries;
        private readonly HashSet<string> _coveredRelativePaths;

        /// <summary>Path of the metadata file the index has been read from.</summary>
        public string FilePath
        {
            get;
        }

        /// <summary>
        /// When the index has been built, taken from the file system: nothing is stored inside the
        /// files, because they are meant to be committed.
        /// </summary>
        public DateTime GenerateDateTime
        {
            get;
        }

        /// <summary>
        /// The agent which produced the vectors. Null when the file does not name one, in which
        /// case the user has to be asked which agent to vectorize the query with.
        /// </summary>
        public string? EmbeddingAgentName
        {
            get;
        }

        public string? EmbeddingModel
        {
            get;
        }

        /// <summary>
        /// The model as the server named it when the index was built, which is not always the model
        /// the settings asked for. Null for an index built before this was recorded.
        /// </summary>
        public string? ReportedEmbeddingModel
        {
            get;
        }

        /// <summary>
        /// The vectors of the sentinel sentences, against which a search checks that it is holding
        /// the model which built this index. Null for an index built before fingerprints existed —
        /// such an index is searched without the check rather than refused.
        /// </summary>
        public EmbeddingSpaceFingerprint? Fingerprint
        {
            get;
        }

        /// <summary>
        /// The measured scale of similarity of this index, see <see cref="EmbeddingCalibration"/>.
        /// Null when the index has not been calibrated, in which case the search falls back to
        /// counts alone and applies no threshold at all.
        /// </summary>
        public EmbeddingCalibration? Calibration
        {
            get;
        }

        public int Dimensions
        {
            get;
        }

        public IReadOnlyList<EmbeddingIndexEntry> Entries => _entries;

        /// <summary>
        /// Every path mentioned by `outlines.json`, including the nodes which have no vector. Tells
        /// "this file is in the index and simply did not match" from "this file was never indexed",
        /// which are worth very different things to the user.
        /// </summary>
        public IReadOnlyCollection<string> CoveredRelativePaths => _coveredRelativePaths;

        public EmbeddingIndex(
            string filePath,
            DateTime generateDateTime,
            string? embeddingAgentName,
            string? embeddingModel,
            int dimensions,
            List<EmbeddingIndexEntry> entries,
            HashSet<string> coveredRelativePaths,
            string? reportedEmbeddingModel = null,
            EmbeddingSpaceFingerprint? fingerprint = null,
            EmbeddingCalibration? calibration = null
            )
        {
            FilePath = filePath;
            GenerateDateTime = generateDateTime;
            EmbeddingAgentName = embeddingAgentName;
            EmbeddingModel = embeddingModel;
            Dimensions = dimensions;
            _entries = entries;
            _coveredRelativePaths = coveredRelativePaths;
            ReportedEmbeddingModel = reportedEmbeddingModel;
            Fingerprint = fingerprint;
            Calibration = calibration;
        }

        /// <summary>
        /// Joins the two halves of the index into something searchable. Vectors whose node is gone
        /// are dropped: the two files have been regenerated separately, or a merge has kept one
        /// side of each, and a hit which cannot be traced back to a file is of no use to anybody.
        /// </summary>
        public static EmbeddingIndex Build(
            EmbeddingIndexMetadata metadata,
            IReadOnlyList<OutlineItselfJsonObject> outlines,
            IReadOnlyList<Json.EmbeddingItselfJsonObject> vectors,
            CancellationToken cancellationToken = default
            )
        {
            if (metadata is null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (outlines is null)
            {
                throw new ArgumentNullException(nameof(outlines));
            }

            if (vectors is null)
            {
                throw new ArgumentNullException(nameof(vectors));
            }

            //file paths come from Windows and are compared against the paths of solution items,
            //which may well differ in case
            var covered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var outlineById = new Dictionary<Guid, OutlineItselfJsonObject>();

            foreach (var outline in outlines)
            {
                outlineById[outline.Id] = outline;

                if (!string.IsNullOrEmpty(outline.RelativePath))
                {
                    covered.Add(outline.RelativePath);
                }
            }

            var entries = new List<EmbeddingIndexEntry>(vectors.Count);
            var dimensions = 0;

            for (var i = 0; i < vectors.Count; i++)
            {
                if ((i % _cancellationCheckInterval) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var vector = vectors[i];

                if (!outlineById.TryGetValue(vector.Id, out var outline))
                {
                    continue;
                }

                if (vector.Embedding is null || vector.Embedding.Length == 0)
                {
                    continue;
                }

                if (dimensions == 0)
                {
                    dimensions = vector.Embedding.Length;
                }

                entries.Add(
                    new EmbeddingIndexEntry(
                        outline.Id,
                        outline.Kind,
                        outline.RelativePath,
                        outline.Target,
                        outline.OutlineText,
                        vector.Embedding
                        )
                    );
            }

            return new EmbeddingIndex(
                metadata.FilePath,
                metadata.GenerateDateTime,
                metadata.EmbeddingAgentName,
                metadata.EmbeddingModel,
                dimensions > 0 ? dimensions : metadata.Dimensions,
                entries,
                covered,
                metadata.ReportedEmbeddingModel,
                metadata.Fingerprint,
                metadata.Calibration
                );
        }

        public bool IsCovered(
            string relativePath
            )
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return false;
            }

            return _coveredRelativePaths.Contains(relativePath);
        }

        /// <summary>
        /// The <paramref name="topK"/> entries closest to <paramref name="queryVector"/>, best
        /// first. A plain linear scan: even a solution the size of FreeAIr itself gives a few
        /// thousand entries, which is a couple of milliseconds — an approximate index would only
        /// add a way to return wrong answers.
        /// </summary>
        public List<(EmbeddingIndexEntry Entry, float Score)> Search(
            float[] queryVector,
            int topK,
            CancellationToken cancellationToken = default
            )
        {
            if (queryVector is null)
            {
                throw new ArgumentNullException(nameof(queryVector));
            }

            var result = new List<(EmbeddingIndexEntry Entry, float Score)>();

            if (topK <= 0 || _entries.Count == 0)
            {
                return result;
            }

            //the caller may hand over a raw vector straight from the provider; normalizing a copy
            //keeps the score a cosine without touching what the caller owns
            var query = (float[])queryVector.Clone();
            if (!VectorCodec.NormalizeInPlace(query))
            {
                return result;
            }

            for (var i = 0; i < _entries.Count; i++)
            {
                if ((i % _cancellationCheckInterval) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                var entry = _entries[i];

                //a vector of another length means another model, and a dot product of the two is
                //not a similarity of anything
                if (entry.Vector.Length != query.Length)
                {
                    continue;
                }

                result.Add(
                    (entry, VectorCodec.DotProduct(entry.Vector, query))
                    );
            }

            result.Sort(
                (a, b) =>
                {
                    var r = b.Score.CompareTo(a.Score);
                    if (r != 0)
                    {
                        return r;
                    }

                    //equal scores are common among the degenerate outlines, and List.Sort is not
                    //stable, so without this the same query would return a different order
                    return a.Entry.Id.CompareTo(b.Entry.Id);
                }
                );

            if (result.Count > topK)
            {
                result.RemoveRange(topK, result.Count - topK);
            }

            return result;
        }
    }

    /// <summary>
    /// What the metadata file of the index says, without the outlines and the vectors. Cheap enough
    /// to be read in the middle of a dialog: it is a few hundred bytes.
    /// </summary>
    public sealed class EmbeddingIndexMetadata
    {
        public string FilePath
        {
            get;
        }

        public DateTime GenerateDateTime
        {
            get;
        }

        public string? EmbeddingAgentName
        {
            get;
        }

        public string? EmbeddingModel
        {
            get;
        }

        public int Dimensions
        {
            get;
        }

        /// <summary>The model as the server named it, see <see cref="EmbeddingIndex.ReportedEmbeddingModel"/>.</summary>
        public string? ReportedEmbeddingModel
        {
            get;
        }

        public EmbeddingSpaceFingerprint? Fingerprint
        {
            get;
        }

        public EmbeddingCalibration? Calibration
        {
            get;
        }

        public EmbeddingIndexMetadata(
            string filePath,
            DateTime generateDateTime,
            string? embeddingAgentName,
            string? embeddingModel,
            int dimensions,
            string? reportedEmbeddingModel = null,
            EmbeddingSpaceFingerprint? fingerprint = null,
            EmbeddingCalibration? calibration = null
            )
        {
            FilePath = filePath;
            GenerateDateTime = generateDateTime;
            EmbeddingAgentName = embeddingAgentName;
            EmbeddingModel = embeddingModel;
            Dimensions = dimensions;
            ReportedEmbeddingModel = reportedEmbeddingModel;
            Fingerprint = fingerprint;
            Calibration = calibration;
        }
    }
}
