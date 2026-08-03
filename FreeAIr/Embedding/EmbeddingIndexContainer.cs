using FreeAIr.Helper;
using FreeAIr.NLOutline.Tree;
using FreeAIr.Options2;
using Microsoft.VisualStudio.Threading;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace FreeAIr.Embedding
{
    /// <summary>
    /// The owner of the loaded embedding index.
    ///
    /// This is a MEF singleton: obtain it via `IComponentModel.GetService&lt;EmbeddingIndexContainer&gt;()`,
    /// never construct it. It exists because the index is expensive to read — megabytes of vectors
    /// for a real solution — while a natural language search asks for it on every run, and the
    /// GetAllSolutionFiles MCP tool on every call.
    ///
    /// This class is where the index meets Visual Studio; everything about the format itself lives
    /// in FreeAIr.Rag, which knows nothing about the IDE and can therefore be tested.
    /// </summary>
    [Export(typeof(EmbeddingIndexContainer))]
    public sealed class EmbeddingIndexContainer
    {
        /// <summary>
        /// Serializes the loads. Two searches started back to back would otherwise both miss the
        /// cache and read the same hundreds of megabytes twice.
        /// </summary>
        private readonly SemaphoreSlim _locker = new SemaphoreSlim(1, 1);

        private EmbeddingIndexContent? _content;
        private string? _contentKey;

        /// <summary>
        /// The searchable view of <see cref="_content"/>, and the content it was built from, kept
        /// together in one object so that a reader can never pick up an index which belongs to
        /// another version of the files.
        /// </summary>
        private BuiltIndex? _builtIndex;

        /// <summary>
        /// Bumped by <see cref="Invalidate"/>. A load which started before that must not publish
        /// its result: it describes the state of the disk which has just been declared obsolete.
        /// </summary>
        private int _generation;

        /// <summary>
        /// The files of the current solution, or null when there is no solution or it has no index.
        /// </summary>
        public static async Task<EmbeddingIndexFileSet?> TryGetFileSetAsync(
            )
        {
            var filePath = await FreeAIrOptions.ComposeEmbeddingsFilePathAsync();
            if (string.IsNullOrEmpty(filePath))
            {
                return null;
            }

            var fileSet = new EmbeddingIndexFileSet(filePath!);
            return fileSet.AllFilesExist()
                ? fileSet
                : null
                ;
        }

        /// <summary>
        /// Whether the index files are on disk at all, without reading them. Used to enable the
        /// `Use RAG` checkbox.
        /// </summary>
        public static async Task<bool> IndexExistsAsync(
            )
        {
            return (await TryGetFileSetAsync()) is not null;
        }

        /// <summary>
        /// Reads the metadata file alone, leaving the outlines and the vectors on disk. Lets the
        /// caller find out which agent has built the index, and when, before committing to loading
        /// it.
        ///
        /// Returns null when the solution has no index or the file cannot be read.
        /// </summary>
        public static async Task<EmbeddingIndexMetadata?> TryReadMetadataAsync(
            CancellationToken cancellationToken = default
            )
        {
            try
            {
                var fileSet = await TryGetFileSetAsync();
                if (fileSet is null)
                {
                    return null;
                }

                return await EmbeddingIndexReader.TryReadMetadataAsync(
                    fileSet,
                    cancellationToken
                    );
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception excp)
            {
                //a damaged index is not a reason to break the dialog which asks about it
                excp.ActivityLogException();
                return null;
            }
        }

        /// <summary>
        /// The index, read from disk when it is missing from the cache or the files have changed
        /// since it was read. Returns null when the solution has no index, or has an incomplete
        /// one — the caller is expected to tell the user rather than to search over nothing.
        ///
        /// Leaves the caller on a background thread.
        /// </summary>
        public async Task<EmbeddingIndex?> GetAsync(
            IProgress<EmbeddingIndexLoadProgress>? progress = null,
            CancellationToken cancellationToken = default
            )
        {
            var content = await GetContentAsync(true, progress, cancellationToken);
            if (content is null)
            {
                return null;
            }

            var built = _builtIndex;
            if (built is not null && ReferenceEquals(built.Content, content))
            {
                return built.Index;
            }

            var index = content.BuildIndex(cancellationToken);
            _builtIndex = new BuiltIndex(content, index);

            return index;
        }

        /// <summary>
        /// The outline tree of the index, freshly built on every call.
        ///
        /// Deliberately not cached, unlike the parsed files it is made of: the callers of this one
        /// go on to modify the tree — the index builder reuses the nodes of the files it is not
        /// going to rescan — and handing all of them the same instance would let one search corrupt
        /// the next. Assembling it costs a walk over the nodes, while the megabytes of json behind
        /// them are read once.
        ///
        /// <paramref name="withVectors"/> off skips the largest file of the three; a caller which
        /// only needs the outline texts must not pay for the vectors.
        /// </summary>
        public async Task<OutlineNode?> GetOutlineTreeAsync(
            bool withVectors,
            IProgress<EmbeddingIndexLoadProgress>? progress = null,
            CancellationToken cancellationToken = default
            )
        {
            var content = await GetContentAsync(withVectors, progress, cancellationToken);
            if (content is null)
            {
                return null;
            }

            return content.Json.BuildOutlineTree();
        }

        /// <summary>
        /// Drops everything cached. The files are watched through their write time anyway, so this
        /// is only needed to release the memory.
        /// </summary>
        public void Invalidate(
            )
        {
            //deliberately without the semaphore: this is callable from the UI thread, and waiting
            //there for a load of hundreds of megabytes to finish would freeze Visual Studio. The
            //generation is what stops a load running right now from publishing a stale result.
            Interlocked.Increment(ref _generation);

            _contentKey = null;
            _content = null;
            _builtIndex = null;
        }

        private async Task<EmbeddingIndexContent?> GetContentAsync(
            bool withVectors,
            IProgress<EmbeddingIndexLoadProgress>? progress,
            CancellationToken cancellationToken
            )
        {
            var fileSet = await TryGetFileSetAsync();
            if (fileSet is null)
            {
                return null;
            }

            //reading the index has no business on the UI thread: it is file IO plus a join over
            //thousands of nodes, and it is started from a view model
            await TaskScheduler.Default;

            var key = fileSet.BuildVersionKey();

            await _locker.WaitAsync(cancellationToken);
            try
            {
                var cached = _content;
                if (cached is not null
                    && string.Equals(_contentKey, key, StringComparison.Ordinal)
                    && (cached.HasVectors || !withVectors)
                    )
                {
                    return cached;
                }

                var generation = Volatile.Read(ref _generation);

                var content = await EmbeddingIndexReader.TryReadContentAsync(
                    fileSet,
                    withVectors,
                    progress,
                    cancellationToken
                    );

                if (content is not null && Volatile.Read(ref _generation) == generation)
                {
                    //the key is the one taken before the load on purpose: had the files been
                    //rewritten while we were reading them, we hold a mix of the two versions, and
                    //it must not be served to anyone else. The next call recomputes the key,
                    //misses, and rereads.
                    _content = content;
                    _contentKey = key;
                }

                return content;
            }
            finally
            {
                _locker.Release();
            }
        }

        private sealed class BuiltIndex
        {
            public EmbeddingIndexContent Content
            {
                get;
            }

            public EmbeddingIndex Index
            {
                get;
            }

            public BuiltIndex(
                EmbeddingIndexContent content,
                EmbeddingIndex index
                )
            {
                Content = content;
                Index = index;
            }
        }
    }
}
