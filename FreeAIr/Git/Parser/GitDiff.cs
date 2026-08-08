using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeAIr.Git.Parser
{
    /// <summary>
    /// A parsed unified git diff: the raw diff text alongside the per-file changes
    /// (<see cref="GitDiffFile"/>) extracted from it. Built by <see cref="GitDiffCreator"/> from the
    /// text <see cref="GitDiffCollector"/> collects, and consumed wherever FreeAIr needs to reason
    /// about which files and lines changed rather than just the raw patch text.
    /// </summary>
    public sealed class GitDiff
    {
        /// <summary>
        /// The per-file changes parsed out of <see cref="Diff"/>.
        /// </summary>
        private readonly List<GitDiffFile> _files;
        /// <summary>
        /// The repository root the relative paths in the diff are resolved against.
        /// </summary>
        private readonly string _rootPath;

        /// <summary>
        /// The raw unified diff text this instance was parsed from.
        /// </summary>
        public string Diff
        {
            get;
        }

        /// <summary>
        /// The changes, one entry per file touched by the diff.
        /// </summary>
        public IReadOnlyList<GitDiffFile> Files => _files;


        public GitDiff(
            string rootPath,
            string diff
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

            _rootPath = rootPath;
            Diff = diff;

            _files = DiffPatch.DiffParserHelper
                .Parse(diff, Environment.NewLine)
                .Select(d => new GitDiffFile(rootPath, d))
                .ToList()
                ;
        }

        /// <summary>
        /// Writes every file's diff to the given <see cref="StringBuilder"/>.
        /// </summary>
        public void WriteTo(StringBuilder sb)
        {
            foreach (var file in _files)
            {
                file.WriteTo(sb);
            }
        }
    }
}
