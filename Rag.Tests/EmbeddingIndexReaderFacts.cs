using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using Xunit;

namespace FreeAIr.Rag.Tests
{
    public sealed class EmbeddingIndexReaderFacts
    {
        [Fact]
        public async Task Reading_the_metadata_does_not_touch_the_rest()
        {
            using var folder = new TempFolder();
            await WriteAsync(folder);

            var fileSet = new EmbeddingIndexFileSet(folder.IndexFilePath);

            //the metadata is a few hundred bytes and is read in the middle of a dialog; hold the
            //big files open to prove they are not read here
            using (File.Open(fileSet.VectorsFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            using (File.Open(fileSet.OutlinesFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var metadata = await EmbeddingIndexReader.TryReadMetadataAsync(fileSet);

                Assert.NotNull(metadata);
                Assert.Equal("the-agent", metadata!.EmbeddingAgentName);
                Assert.Equal("the-model", metadata.EmbeddingModel);
                Assert.Equal(8, metadata.Dimensions);
            }
        }

        [Fact]
        public async Task An_outline_only_read_leaves_the_vectors_on_disk()
        {
            //this is what the GetAllSolutionFiles MCP tool needs, and the vectors are the megabytes
            using var folder = new TempFolder();
            await WriteAsync(folder);

            var fileSet = new EmbeddingIndexFileSet(folder.IndexFilePath);

            using (File.Open(fileSet.VectorsFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var content = await EmbeddingIndexReader.TryReadContentAsync(fileSet, false);

                Assert.NotNull(content);
                Assert.False(content!.HasVectors);
                Assert.NotEmpty(content.Json.Outlines!.Outlines);
                Assert.NotNull(content.Json.BuildOutlineTree());
            }
        }

        [Fact]
        public async Task The_reader_says_what_it_is_doing()
        {
            using var folder = new TempFolder();
            await WriteAsync(folder);

            var progress = new RecordingProgress();

            var index = await EmbeddingIndexReader.TryReadAsync(
                new EmbeddingIndexFileSet(folder.IndexFilePath),
                progress
                );

            Assert.NotNull(index);
            Assert.Equal(
                new[]
                {
                    EmbeddingIndexLoadPhaseEnum.ReadingMetadata,
                    EmbeddingIndexLoadPhaseEnum.ReadingOutlines,
                    EmbeddingIndexLoadPhaseEnum.ReadingVectors,
                    EmbeddingIndexLoadPhaseEnum.Preparing
                },
                progress.Reports.Select(r => r.Phase).Distinct()
                );

            var vectors = progress.Reports.Where(r => r.Phase == EmbeddingIndexLoadPhaseEnum.ReadingVectors).ToList();
            Assert.All(vectors, r => Assert.True(r.Total > 0L));
            Assert.True(vectors[^1].Processed > 0L);
        }

        [Fact]
        public async Task Reading_can_be_cancelled()
        {
            using var folder = new TempFolder();
            await WriteAsync(folder);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => EmbeddingIndexReader.TryReadAsync(
                    new EmbeddingIndexFileSet(folder.IndexFilePath),
                    null,
                    cts.Token
                    )
                );
        }

        [Fact]
        public async Task Reading_stops_as_soon_as_it_is_cancelled_in_the_middle()
        {
            using var folder = new TempFolder();
            await WriteAsync(folder);

            using var cts = new CancellationTokenSource();

            //cancel when the vectors start coming in, i.e. inside the longest step of the load
            var progress = new CancellingProgress(cts);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => EmbeddingIndexReader.TryReadAsync(
                    new EmbeddingIndexFileSet(folder.IndexFilePath),
                    progress,
                    cts.Token
                    )
                );
        }

        [Fact]
        public async Task A_damaged_line_of_the_vectors_file_does_not_cost_the_whole_index()
        {
            //a line based format is used exactly so that this is survivable
            using var folder = new TempFolder();
            await WriteAsync(folder);

            var fileSet = new EmbeddingIndexFileSet(folder.IndexFilePath);

            var lines = await File.ReadAllLinesAsync(fileSet.VectorsFilePath);
            Assert.True(lines.Length > 2);

            lines[1] = "{ \"Id\": \"" + Guid.NewGuid() + "\", \"V\": \"@@@ not base64 @@@\" }";
            await File.WriteAllLinesAsync(fileSet.VectorsFilePath, lines);

            var index = await EmbeddingIndexReader.TryReadAsync(fileSet);

            Assert.NotNull(index);
            Assert.Equal(lines.Length - 1, index!.Entries.Count);
        }

        private static async Task WriteAsync(
            TempFolder folder
            )
        {
            var json = new EmbeddingOutlineJsonObject(
                IndexFileFacts.SampleTree(),
                "the-agent",
                "the-model",
                "https://example.invalid/v1"
                );

            await json.SerializeAsync(folder.IndexFilePath, default);
        }

        private sealed class RecordingProgress : IProgress<EmbeddingIndexLoadProgress>
        {
            public List<EmbeddingIndexLoadProgress> Reports
            {
                get;
            } = new();

            public void Report(
                EmbeddingIndexLoadProgress value
                )
            {
                Reports.Add(value);
            }
        }

        private sealed class CancellingProgress : IProgress<EmbeddingIndexLoadProgress>
        {
            private readonly CancellationTokenSource _cts;

            public CancellingProgress(
                CancellationTokenSource cts
                )
            {
                _cts = cts;
            }

            public void Report(
                EmbeddingIndexLoadProgress value
                )
            {
                if (value.Phase == EmbeddingIndexLoadPhaseEnum.ReadingVectors)
                {
                    _cts.Cancel();
                }
            }
        }
    }
}
