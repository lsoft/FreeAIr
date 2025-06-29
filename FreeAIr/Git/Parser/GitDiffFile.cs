using DiffPatch.Data;
using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FreeAIr.Git.Parser
{
    public sealed class GitDiffFile
    {
        private const string _devNull = @"/dev/null";

        private readonly FileDiff _diff;
        private readonly List<GitDiffChunk> _chunks;

        public string OriginalFullPath
        {
            get;
        }

        public string NewFullPath
        {
            get;
        }

        public GitDiffFileStatusEnum Status
        {
            get
            {
                if (OriginalFullPath is null)
                {
                    return GitDiffFileStatusEnum.Added;
                }
                if (NewFullPath is null)
                {
                    return GitDiffFileStatusEnum.Deleted;
                }

                return GitDiffFileStatusEnum.Updated;
            }
        }

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

    public enum GitDiffFileStatusEnum
    {
        Added,
        Updated,
        Deleted
    }
}
