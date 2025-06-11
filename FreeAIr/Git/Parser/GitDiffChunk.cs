using DiffPatch.Data;
using FreeAIr.Helper;
using FreeAIr.Shared.Helper;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FreeAIr.Git.Parser
{
    public sealed class GitDiffChunk
    {
        private readonly Chunk _chunk;

        public IReadOnlyList<LineDiff> AddedLines
        {
            get;
        }

        public IReadOnlyList<LineDiff> DeletedLines
        {
            get;
        }

        public ChunkRange OriginalRange => _chunk.RangeInfo.OriginalRange;

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
