using DiffPatch.Data;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeAIr.Git.Parser
{
    /// <summary>
    /// One `@@ ... @@` hunk of a file's diff: the line ranges it touches in the original and new
    /// version, and which lines within it were added or deleted. Used to scope AI context to just
    /// the changed lines, e.g. when adding natural language outlines to a diff's files.
    /// </summary>
    public sealed class GitDiffChunk
    {
        /// <summary>
        /// The underlying parsed chunk this instance wraps.
        /// </summary>
        private readonly Chunk _chunk;

        /// <summary>
        /// The lines added by this chunk.
        /// </summary>
        public IReadOnlyList<LineDiff> AddedLines
        {
            get;
        }

        /// <summary>
        /// The lines deleted by this chunk.
        /// </summary>
        public IReadOnlyList<LineDiff> DeletedLines
        {
            get;
        }

        /// <summary>
        /// The line range this chunk covers in the original version of the file.
        /// </summary>
        public ChunkRange OriginalRange => _chunk.RangeInfo.OriginalRange;

        /// <summary>
        /// The line range this chunk covers in the new version of the file.
        /// </summary>
        public ChunkRange NewRange => _chunk.RangeInfo.NewRange;

        public GitDiffChunk(Chunk chunk)
        {
            if (chunk is null)
            {
                throw new ArgumentNullException(nameof(chunk));
            }

            _chunk = chunk;

            AddedLines = chunk.Changes
                .Where(l => l.Type == LineChangeType.Add)
                .ToList()
                ;
            DeletedLines = chunk.Changes
                .Where(l => l.Type == LineChangeType.Delete)
                .ToList()
                ;
        }

        /// <summary>
        /// Writes this chunk in unified diff format (the `@@ ... @@` header plus the added, deleted
        /// and context lines).
        /// </summary>
        public void WriteTo(StringBuilder sb)
        {
            Console.WriteLine($"@@ -{_chunk.RangeInfo.OriginalRange.StartLine},{_chunk.RangeInfo.OriginalRange.LineCount} +{_chunk.RangeInfo.NewRange.StartLine},{_chunk.RangeInfo.NewRange.LineCount} @@");
            foreach (var change in _chunk.Changes)
            {
                switch (change.Type)
                {
                    case DiffPatch.Data.LineChangeType.Normal:
                        Console.WriteLine($" {change.Content}");
                        break;
                    case DiffPatch.Data.LineChangeType.Add:
                        Console.WriteLine($"+{change.Content}");
                        break;
                    case DiffPatch.Data.LineChangeType.Delete:
                        Console.WriteLine($"-{change.Content}");
                        break;
                }
            }

        }

        /// <summary>
        /// The start line and line count, in the new version of the file, spanning from the first to
        /// the last added-or-deleted line of this chunk. Used to build the line range that should be
        /// added as scope when a chat context item is created for the changed file.
        /// </summary>
        public (int StartLine, int LineCount) GetDiffChunk()
        {
            var startLine = NewRange.StartLine;
            var firstChangeLineIndex = _chunk.Changes.FindIndex(
                ch => ch.Type.In(LineChangeType.Add, LineChangeType.Delete)
                );
            if (firstChangeLineIndex < 0)
            {
                firstChangeLineIndex = 0;
            }

            var lastChangeLineIndex = _chunk.Changes.FindLastIndex(
                ch => ch.Type.In(LineChangeType.Add, LineChangeType.Delete)
                );
            if (lastChangeLineIndex < 0)
            {
                lastChangeLineIndex = 0;
            }

            return (startLine + firstChangeLineIndex - 1, NewRange.LineCount - (_chunk.Changes.Count - lastChangeLineIndex) - firstChangeLineIndex + 1);
        }
    }
}
