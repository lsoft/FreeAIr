using DiffPatch.Data;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeAIr.Git.Parser
{
    /// <summary>
    /// One file's worth of changes from a <see cref="GitDiff"/>: its original and new absolute
    /// paths, whether it was added, updated or deleted, and the individual <see cref="GitDiffChunk"/>
    /// hunks that changed it.
    /// </summary>
    public sealed class GitDiffFile
    {
        /// <summary>
        /// The placeholder git uses for the missing side of an add or delete diff.
        /// </summary>
        private const string _devNull = @"/dev/null";

        /// <summary>
        /// The underlying parsed file diff this instance wraps.
        /// </summary>
        private readonly FileDiff _diff;
        /// <summary>
        /// The hunks of this file's diff.
        /// </summary>
        private readonly List<GitDiffChunk> _chunks;

        /// <summary>
        /// The absolute path of the file before this diff, or empty when the file is newly added.
        /// </summary>
        public string OriginalFullPath
        {
            get;
        }

        /// <summary>
        /// The absolute path of the file after this diff, or empty when the file was deleted.
        /// </summary>
        public string NewFullPath
        {
            get;
        }

        /// <summary>
        /// Whether this file was added, updated or deleted, derived from which of
        /// <see cref="OriginalFullPath"/> and <see cref="NewFullPath"/> is empty.
        /// </summary>
        public GitDiffFileStatusEnum Status
        {
            get
            {
                if (string.IsNullOrEmpty(OriginalFullPath))
                {
                    return GitDiffFileStatusEnum.Added;
                }
                if (string.IsNullOrEmpty(NewFullPath))
                {
                    return GitDiffFileStatusEnum.Deleted;
                }

                return GitDiffFileStatusEnum.Updated;
            }
        }

        /// <summary>
        /// The hunks that changed this file.
        /// </summary>
        public IReadOnlyList<GitDiffChunk> Chunks => _chunks;

        public GitDiffFile(
            string rootPath,
            FileDiff diff
            )
        {
            if (rootPath is null)
            {
                throw new ArgumentNullException(nameof(rootPath));
            }

            if (diff is null)
            {
                throw new ArgumentNullException(nameof(diff));
            }

            _diff = diff;

            OriginalFullPath =
                diff.From == _devNull
                ? string.Empty
                : Path.GetFullPath(Path.Combine(rootPath, diff.From))
                ;
            NewFullPath =
                diff.To == _devNull
                ? string.Empty
                : Path.GetFullPath(Path.Combine(rootPath, diff.To))
                ;

            _chunks = diff.Chunks
                .Select(c => new GitDiffChunk(c))
                .ToList()
                ;
        }

        /// <summary>
        /// Writes this file's header line and every hunk to the given <see cref="StringBuilder"/>.
        /// </summary>
        public void WriteTo(StringBuilder sb)
        {
            switch (_diff.Type)
            {
                case DiffPatch.Data.FileChangeType.Modified:
                    Console.WriteLine($"{_diff.From} -> {_diff.To} ({_diff.Type})");
                    break;
                case DiffPatch.Data.FileChangeType.Add:
                    Console.WriteLine($"-> {_diff.To} ({_diff.Type})");
                    break;
                case DiffPatch.Data.FileChangeType.Delete:
                    Console.WriteLine($"{_diff.From} ({_diff.Type})");
                    break;
            }

            foreach (var chunk in _chunks)
            {
                chunk.WriteTo(sb);
            }
        }

        /// <summary>
        /// The changed line ranges of every hunk in this file, in the new version of the file. Used
        /// to scope AI chat context to just the touched lines.
        /// </summary>
        public List<(int StartLine, int LineCount)> GetDiffChunks()
        {
            var result = new List<(int StartLine, int LineCount)>();

            foreach (var chunk in Chunks)
            {
                result.Add(
                    chunk.GetDiffChunk()
                    );
            }

            return result;
        }
    }

    /// <summary>
    /// How a file was changed by a git diff.
    /// </summary>
    public enum GitDiffFileStatusEnum
    {
        /// <summary>
        /// The file is new; it had no original version.
        /// </summary>
        Added,
        /// <summary>
        /// The file existed before and after, with content changes.
        /// </summary>
        Updated,
        /// <summary>
        /// The file was removed; it has no new version.
        /// </summary>
        Deleted
    }
}
