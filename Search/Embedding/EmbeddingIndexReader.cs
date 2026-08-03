using FreeAIr.Embedding.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Embedding
{
    public enum EmbeddingIndexLoadPhaseEnum
    {
        ReadingMetadata = 1,
        ReadingOutlines = 2,
        ReadingVectors = 3,
        Preparing = 4
    }

    /// <summary>
    /// A step of <see cref="EmbeddingIndexReader"/>, reported so that the results panel can show
    /// something while a multi megabyte index is being read. <see cref="Total"/> is zero when the
    /// length of the phase is not known in advance — the progress bar goes indeterminate.
    /// </summary>
    public sealed class EmbeddingIndexLoadProgress
    {
        public EmbeddingIndexLoadPhaseEnum Phase
        {
            get;
        }

        /// <summary>Bytes for <see cref="EmbeddingIndexLoadPhaseEnum.ReadingVectors"/>, nodes elsewhere.</summary>
        public long Processed
        {
            get;
        }

        public long Total
        {
            get;
        }

        public EmbeddingIndexLoadProgress(
            EmbeddingIndexLoadPhaseEnum phase,
            long processed = 0L,
            long total = 0L
            )
        {
            Phase = phase;
            Processed = processed;
            Total = total;
        }
    }

    /// <summary>
    /// Reads the index files off the disk. Everything here is cancellable at every step, including
    /// the two large reads: the vectors file of a real solution is tens of megabytes, and a user
    /// who has pressed Cancel is not willing to wait for it.
    /// </summary>
    public static class EmbeddingIndexReader
    {
        /// <summary>
        /// Reads the metadata file alone, leaving the outlines and the vectors on disk. Lets the
        /// caller find out which agent has built the index, and when, before committing to loading
        /// it — asking the user about the agent is a modal dialog, and it has to happen before the
        /// search starts, not in the middle of it.
        ///
        /// Returns null when the set of files is incomplete or the metadata cannot be read.
        /// </summary>
        public static async Task<EmbeddingIndexMetadata?> TryReadMetadataAsync(
            EmbeddingIndexFileSet fileSet,
            CancellationToken cancellationToken = default
            )
        {
            if (fileSet is null)
            {
                throw new ArgumentNullException(nameof(fileSet));
            }

            if (!fileSet.AllFilesExist())
            {
                return null;
            }

            var json = await EmbeddingOutlineJsonObject.DeserializeAsync(
                fileSet.MetadataFilePath,
                false,
                cancellationToken
                );
            if (json is null)
            {
                return null;
            }

            return CreateMetadata(
                fileSet,
                json
                );
        }

        /// <summary>
        /// The whole index, ready to be searched. Returns null when the set of files is incomplete
        /// or the metadata cannot be read — the caller is expected to tell the user rather than to
        /// search over nothing.
        /// </summary>
        public static async Task<EmbeddingIndex?> TryReadAsync(
            EmbeddingIndexFileSet fileSet,
            IProgress<EmbeddingIndexLoadProgress>? progress = null,
            CancellationToken cancellationToken = default
            )
        {
            if (fileSet is null)
            {
                throw new ArgumentNullException(nameof(fileSet));
            }

            var content = await TryReadContentAsync(
                fileSet,
                true,
                progress,
                cancellationToken
                );
            if (content is null)
            {
                return null;
            }

            progress?.Report(
                new EmbeddingIndexLoadProgress(EmbeddingIndexLoadPhaseEnum.Preparing)
                );

            return EmbeddingIndex.Build(
                content.Metadata,
                content.Json.Outlines!.Outlines,
                content.Json.Embeddings!.Embeddings,
                cancellationToken
                );
        }

        /// <summary>
        /// The parsed files themselves. <paramref name="withVectors"/> off skips the largest file
        /// of the three, which is what a caller who only wants the outline texts should do.
        /// </summary>
        public static async Task<EmbeddingIndexContent?> TryReadContentAsync(
            EmbeddingIndexFileSet fileSet,
            bool withVectors,
            IProgress<EmbeddingIndexLoadProgress>? progress = null,
            CancellationToken cancellationToken = default
            )
        {
            if (fileSet is null)
            {
                throw new ArgumentNullException(nameof(fileSet));
            }

            if (!fileSet.AllFilesExist())
            {
                return null;
            }

            progress?.Report(
                new EmbeddingIndexLoadProgress(EmbeddingIndexLoadPhaseEnum.ReadingMetadata)
                );

            //`full: false` reads the metadata only, and the siblings are loaded below one by one,
            //so that each of them can be reported separately
            var json = await EmbeddingOutlineJsonObject.DeserializeAsync(
                fileSet.MetadataFilePath,
                false,
                cancellationToken
                );
            if (json is null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report(
                new EmbeddingIndexLoadProgress(EmbeddingIndexLoadPhaseEnum.ReadingOutlines)
                );

            await json.LoadOutlinesAsync(cancellationToken);

            if (json.Outlines is null)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (withVectors)
            {
                var vectorsFileLength = new FileInfo(fileSet.VectorsFilePath).Length;

                progress?.Report(
                    new EmbeddingIndexLoadProgress(
                        EmbeddingIndexLoadPhaseEnum.ReadingVectors,
                        0L,
                        vectorsFileLength
                        )
                    );

                await json.LoadEmbeddingsAsync(
                    new BytesReadProgress(progress, vectorsFileLength),
                    cancellationToken
                    );
            }

            var metadata = CreateMetadata(
                fileSet,
                json
                );

            return new EmbeddingIndexContent(fileSet, metadata, json);
        }

        /// <summary>
        /// The metadata as the rest of the code wants it. The fingerprint and the calibration are
        /// optional on purpose: an index written before they existed, or one whose fields have been
        /// damaged by a merge, is searched without them rather than refused.
        /// </summary>
        private static EmbeddingIndexMetadata CreateMetadata(
            EmbeddingIndexFileSet fileSet,
            EmbeddingOutlineJsonObject json
            )
        {
            return new EmbeddingIndexMetadata(
                fileSet.MetadataFilePath,
                json.GenerateDateTime,
                json.EmbeddingAgentName,
                json.EmbeddingModel,
                json.EmbeddingDimensions,
                json.ReportedEmbeddingModel,
                EmbeddingSpaceFingerprint.FromEncoded(json.SpaceFingerprint),
                json.Calibration?.ToCalibration()
                );
        }

        /// <summary>
        /// Turns the byte counter of the vectors reader into the progress this class reports.
        /// Deliberately not a <see cref="Progress{T}"/>: that one posts every report through the
        /// synchronization context, which both reorders them and floods the UI thread with a
        /// callback per line of the file.
        /// </summary>
        private sealed class BytesReadProgress : IProgress<long>
        {
            private readonly IProgress<EmbeddingIndexLoadProgress>? _target;
            private readonly long _total;

            public BytesReadProgress(
                IProgress<EmbeddingIndexLoadProgress>? target,
                long total
                )
            {
                _target = target;
                _total = total;
            }

            public void Report(
                long value
                )
            {
                _target?.Report(
                    new EmbeddingIndexLoadProgress(
                        EmbeddingIndexLoadPhaseEnum.ReadingVectors,
                        value,
                        _total
                        )
                    );
            }
        }
    }

    /// <summary>
    /// The parsed index files, before anything has been derived from them. Both the search view
    /// (<see cref="EmbeddingIndex"/>) and the outline tree are built out of this, which is what
    /// lets one read of the disk serve both.
    /// </summary>
    public sealed class EmbeddingIndexContent
    {
        public EmbeddingIndexFileSet FileSet
        {
            get;
        }

        public EmbeddingIndexMetadata Metadata
        {
            get;
        }

        public EmbeddingOutlineJsonObject Json
        {
            get;
        }

        /// <summary>Whether the vectors file has been read, see the reader's parameter.</summary>
        public bool HasVectors => Json.Embeddings is not null;

        public EmbeddingIndexContent(
            EmbeddingIndexFileSet fileSet,
            EmbeddingIndexMetadata metadata,
            EmbeddingOutlineJsonObject json
            )
        {
            FileSet = fileSet;
            Metadata = metadata;
            Json = json;
        }

        public EmbeddingIndex BuildIndex(
            CancellationToken cancellationToken = default
            )
        {
            return EmbeddingIndex.Build(
                Metadata,
                Json.Outlines!.Outlines,
                Json.Embeddings?.Embeddings ?? (IReadOnlyList<EmbeddingItselfJsonObject>)Array.Empty<EmbeddingItselfJsonObject>(),
                cancellationToken
                );
        }
    }
}
