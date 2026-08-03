using System;
using System.IO;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// The files an embedding index is made of, and the rules which tie them together.
    ///
    /// There are three, all siblings of `&lt;solution name&gt;_embeddings.json`:
    /// the metadata, the outlines and the vectors. They are meant to be committed, which is why
    /// none of them stores its own path or the paths of the others — the set is derived from
    /// wherever the metadata file turned out to be on this machine.
    /// </summary>
    public sealed class EmbeddingIndexFileSet
    {
        public const string OutlinesPart = "outlines";
        public const string VectorsPart = "embeddings";

        /// <summary>`…\Foo_embeddings.json`.</summary>
        public string MetadataFilePath
        {
            get;
        }

        /// <summary>`…\Foo_embeddings.outlines.json`.</summary>
        public string OutlinesFilePath
        {
            get;
        }

        /// <summary>
        /// `…\Foo_embeddings.embeddings.jsonl`. One vector per line rather than one json array:
        /// this is by far the largest file, so it can be read incrementally with a progress report,
        /// and a single damaged line does not cost the whole index.
        /// </summary>
        public string VectorsFilePath
        {
            get;
        }

        public EmbeddingIndexFileSet(
            string metadataFilePath
            )
        {
            if (metadataFilePath is null)
            {
                throw new ArgumentNullException(nameof(metadataFilePath));
            }

            MetadataFilePath = metadataFilePath;
            OutlinesFilePath = GetSiblingFileName(metadataFilePath, OutlinesPart);
            VectorsFilePath = GetSiblingFileName(metadataFilePath, VectorsPart, ".jsonl");
        }

        public bool AllFilesExist(
            )
        {
            return File.Exists(MetadataFilePath)
                && File.Exists(OutlinesFilePath)
                && File.Exists(VectorsFilePath)
                ;
        }

        /// <summary>
        /// Identifies the version of the index on disk, for use as a cache key. Covers the vectors
        /// and the outlines, not just the metadata: the metadata only changes when the embedding
        /// model does, so keying on it alone would serve a stale index after every regeneration.
        /// </summary>
        public string BuildVersionKey(
            )
        {
            return string.Join(
                "|",
                MetadataFilePath,
                GetWriteTimeTicks(MetadataFilePath).ToString(),
                GetWriteTimeTicks(OutlinesFilePath).ToString(),
                GetWriteTimeTicks(VectorsFilePath).ToString()
                );
        }

        private static long GetWriteTimeTicks(
            string filePath
            )
        {
            try
            {
                return File.GetLastWriteTimeUtc(filePath).Ticks;
            }
            catch (Exception)
            {
                //an unreadable file is a cache miss, not a failure of the whole search
                return 0L;
            }
        }

        /// <summary>
        /// `…\Foo_embeddings.json` + `outlines` =&gt; `…\Foo_embeddings.outlines.json`.
        /// </summary>
        public static string GetSiblingFileName(
            string filePath,
            string part,
            string? extension = null
            )
        {
            if (filePath is null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            var fi = new FileInfo(filePath);

            return Path.Combine(
                fi.Directory.FullName,
                fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length) + "." + part + (extension ?? fi.Extension)
                );
        }
    }
}
