using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreeAIr.Git.Parser
{
    public sealed class GitDiff
    {
        private readonly List<GitDiffFile> _files;
        private readonly string _rootPath;

        public string Diff
        {
            get;
        }

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

        public void WriteTo(StringBuilder sb)
        {
            foreach (var file in _files)
            {
                file.WriteTo(sb);
            }
        }
    }
}
