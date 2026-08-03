using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.NLOutline.Tree;
using Xunit;

namespace FreeAIr.Rag.Tests
{
    /// <summary>
    /// Building the index files out of an outline tree, and reading them back.
    /// </summary>
    public sealed class IndexFileFacts
    {
        [Fact]
        public async Task An_index_is_three_files_and_the_outline_tree_is_not_one_of_them()
        {
            using var folder = new TempFolder();

            await WriteAsync(folder, SampleTree());

            var fileSet = new EmbeddingIndexFileSet(folder.IndexFilePath);

            Assert.True(fileSet.AllFilesExist());
            Assert.EndsWith("Test_embeddings.outlines.json", fileSet.OutlinesFilePath);
            Assert.EndsWith("Test_embeddings.embeddings.jsonl", fileSet.VectorsFilePath);

            Assert.Equal(
                3,
                Directory.GetFiles(folder.Path).Length
                );
            Assert.False(
                File.Exists(Path.Combine(folder.Path, "Test_embeddings.outlineTree.json")),
                "the shape of the tree is derived, it must not be written to disk"
                );
        }

        [Fact]
        public async Task The_files_hold_nothing_which_changes_by_itself()
        {
            using var folder = new TempFolder();

            await WriteAsync(folder, SampleTree());

            var metadata = await File.ReadAllTextAsync(folder.IndexFilePath);

            //these files are committed. A path or a timestamp inside one turns every rebuild into
            //a diff, and every pair of branches into a conflict.
            Assert.DoesNotContain("FilePath", metadata);
            Assert.DoesNotContain("GenerateDateTime", metadata);
            Assert.DoesNotContain(folder.Path, metadata);

            Assert.Contains("\"EmbeddingModel\": \"the-model\"", metadata);
            Assert.Contains("\"EmbeddingDimensions\": 8", metadata);
            Assert.Contains(VectorCodec.Int8Base64EncodingName, metadata);
        }

        [Fact]
        public async Task Two_machines_building_the_same_index_produce_the_same_bytes()
        {
            //the same solution, walked in two different orders: this is what two developers on two
            //branches get, and the files must not differ because of it
            using var first = new TempFolder();
            using var second = new TempFolder();

            await WriteAsync(first, SampleTree(reversed: false));
            await WriteAsync(second, SampleTree(reversed: true));

            foreach (var name in Directory.GetFiles(first.Path).Select(Path.GetFileName))
            {
                Assert.Equal(
                    await File.ReadAllTextAsync(Path.Combine(first.Path, name!)),
                    await File.ReadAllTextAsync(Path.Combine(second.Path, name!))
                    );
            }
        }

        [Fact]
        public async Task Everything_written_can_be_read_back()
        {
            using var folder = new TempFolder();

            var original = SampleTree();
            await WriteAsync(folder, original);

            var read = await EmbeddingOutlineJsonObject.DeserializeAsync(
                folder.IndexFilePath,
                true
                );

            Assert.NotNull(read);
            Assert.Equal("the-agent", read!.EmbeddingAgentName);
            Assert.Equal(8, read.EmbeddingDimensions);

            var originalNodes = original.Flatten();

            Assert.Equal(
                originalNodes.Select(n => n.Id).OrderBy(id => id),
                read.Outlines!.Outlines.Select(o => o.Id).OrderBy(id => id)
                );

            var embeddedIds = originalNodes
                .Where(n => n.Embedding is not null)
                .Select(n => n.Id)
                .OrderBy(id => id)
                .ToList();

            Assert.Equal(
                embeddedIds,
                read.Embeddings!.Embeddings.Select(e => e.Id).OrderBy(id => id)
                );

            //the stored vector is the original one quantized and normalized, so it is the direction
            //which has to survive, not the numbers
            foreach (var stored in read.Embeddings.Embeddings)
            {
                var source = originalNodes.Single(n => n.Id == stored.Id).Embedding!;
                var normalized = (float[])source.Clone();
                VectorCodec.NormalizeInPlace(normalized);

                Assert.True(VectorCodec.DotProduct(normalized, stored.Embedding) > 0.99f);
            }
        }

        [Fact]
        public void An_outline_which_only_repeats_its_own_name_is_not_embedded()
        {
            //this is the majority of the nodes of a real solution, and embedding them would cost
            //most of the index for outlines a plain text search already finds
            var solution = TreeFactory.Solution();
            var project = solution.Project("A\\A.csproj", "A");
            var file = project.File("A\\Thing.cs");
            var type = file.Type("Thing", "Thing");
            var documented = type.Member("Save", "persists the thing into the database");
            var undocumented = type.Member("Load", "Thing.Load");

            var selected = OutlineEmbedder.SelectNodesToEmbed(solution);

            Assert.Contains(documented, selected);
            Assert.DoesNotContain(undocumented, selected);
            Assert.DoesNotContain(type, selected);
            Assert.DoesNotContain(file, selected);
            Assert.DoesNotContain(project, selected);
            Assert.DoesNotContain(solution, selected);
        }

        [Fact]
        public async Task A_node_which_already_has_a_vector_is_not_sent_again()
        {
            var solution = TreeFactory.Solution();
            var file = solution.Project("A\\A.csproj", "A").File("A\\Thing.cs");
            var type = file.Type("Thing", "the thing itself");
            type.Member("Save", "persists the thing");

            var vectorizer = new FakeVectorizer(4);
            await new OutlineEmbedder(vectorizer).GenerateEmbeddingsAsync(solution, default);

            Assert.Single(vectorizer.Requests);
            Assert.Equal(2, vectorizer.Requests[0].Count);

            //a second run has nothing left to do, which is what makes an incremental rebuild cheap
            await new OutlineEmbedder(vectorizer).GenerateEmbeddingsAsync(solution, default);
            Assert.Single(vectorizer.Requests);
        }

        [Fact]
        public async Task A_vectorizer_which_drops_an_input_is_not_believed()
        {
            //vectors are matched to nodes by position. A short answer means every vector after the
            //gap belongs to another node, and an index built out of that is silently wrong for good
            var solution = TreeFactory.Solution();
            var file = solution.Project("A\\A.csproj", "A").File("A\\Thing.cs");
            file.Type("Thing", "the thing itself").Member("Save", "persists the thing");

            var embedder = new OutlineEmbedder(new ShortVectorizer());

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => embedder.GenerateEmbeddingsAsync(solution, default)
                );
        }

        [Fact]
        public async Task The_build_date_comes_from_the_file_system()
        {
            using var folder = new TempFolder();
            await WriteAsync(folder, SampleTree());

            var written = new DateTime(2026, 5, 4, 3, 2, 1, DateTimeKind.Local);
            File.SetLastWriteTime(folder.IndexFilePath, written);

            var metadata = await EmbeddingIndexReader.TryReadMetadataAsync(
                new EmbeddingIndexFileSet(folder.IndexFilePath)
                );

            Assert.NotNull(metadata);
            Assert.Equal(written, metadata!.GenerateDateTime);
        }

        [Fact]
        public async Task An_index_missing_one_of_its_files_is_not_an_index()
        {
            using var folder = new TempFolder();
            await WriteAsync(folder, SampleTree());

            var fileSet = new EmbeddingIndexFileSet(folder.IndexFilePath);
            File.Delete(fileSet.VectorsFilePath);

            Assert.False(fileSet.AllFilesExist());
            Assert.Null(await EmbeddingIndexReader.TryReadMetadataAsync(fileSet));
            Assert.Null(await EmbeddingIndexReader.TryReadAsync(fileSet));
        }

        [Fact]
        public async Task Rewriting_a_file_changes_the_cache_key()
        {
            using var folder = new TempFolder();
            await WriteAsync(folder, SampleTree());

            var fileSet = new EmbeddingIndexFileSet(folder.IndexFilePath);
            var before = fileSet.BuildVersionKey();

            Assert.Equal(before, fileSet.BuildVersionKey());

            File.SetLastWriteTimeUtc(
                fileSet.VectorsFilePath,
                File.GetLastWriteTimeUtc(fileSet.VectorsFilePath).AddMinutes(1)
                );

            Assert.NotEqual(before, fileSet.BuildVersionKey());
        }

        [Fact]
        public async Task A_recalibration_rewrites_the_metadata_and_nothing_else()
        {
            //what the calibration window does when the user saves: the numbers change, the vectors
            //are hours of a model's time and must not be touched
            using var folder = new TempFolder();
            await WriteAsync(folder, SampleTree());

            var fileSet = new EmbeddingIndexFileSet(folder.IndexFilePath);

            var vectorsBefore = await File.ReadAllTextAsync(fileSet.VectorsFilePath);
            var outlinesBefore = await File.ReadAllTextAsync(fileSet.OutlinesFilePath);
            var vectorsWrittenAt = File.GetLastWriteTimeUtc(fileSet.VectorsFilePath);

            var json = await EmbeddingOutlineJsonObject.DeserializeAsync(
                folder.IndexFilePath,
                false
                );

            Assert.NotNull(json);
            Assert.Null(json!.Outlines);
            Assert.Null(json.Embeddings);

            json.Calibration = new CalibrationJsonObject(
                new EmbeddingCalibration(0.42f, 0.7f, 5, 2, 0)
                );

            await json.SerializeMetadataAsync(folder.IndexFilePath, default);

            //the siblings are untouched, down to the write time: a rebuild of them is exactly what
            //this call exists to avoid
            Assert.Equal(vectorsBefore, await File.ReadAllTextAsync(fileSet.VectorsFilePath));
            Assert.Equal(outlinesBefore, await File.ReadAllTextAsync(fileSet.OutlinesFilePath));
            Assert.Equal(vectorsWrittenAt, File.GetLastWriteTimeUtc(fileSet.VectorsFilePath));

            //and the index still reads as a whole, with the new numbers in it
            var index = await EmbeddingIndexReader.TryReadAsync(fileSet);

            Assert.NotNull(index);
            Assert.Equal("the-agent", index!.EmbeddingAgentName);
            Assert.Equal(8, index.Dimensions);
            Assert.NotEmpty(index.Entries);
            Assert.NotNull(index.Calibration);
            Assert.Equal(0.42f, index.Calibration!.NoiseCeiling, 3);
            Assert.Equal(0.7f, index.Calibration.RelevantFloor, 3);
        }

        private static async Task WriteAsync(
            TempFolder folder,
            OutlineNode root
            )
        {
            var json = new EmbeddingOutlineJsonObject(
                root,
                "the-agent",
                "the-model",
                "https://example.invalid/v1"
                );

            await json.SerializeAsync(folder.IndexFilePath, default);
        }

        /// <summary>
        /// Two projects, three files, a few documented members. <paramref name="reversed"/> adds
        /// everything in the opposite order, which must not show up in the files.
        /// </summary>
        internal static OutlineNode SampleTree(
            bool reversed = false
            )
        {
            var solution = TreeFactory.Solution();

            var names = new[] { "Alpha", "Beta" };
            if (reversed)
            {
                Array.Reverse(names);
            }

            foreach (var name in names)
            {
                var project = solution.Project($"{name}\\{name}.csproj", name);

                var files = new[] { "Service", "Model" };
                if (reversed)
                {
                    Array.Reverse(files);
                }

                foreach (var fileName in files)
                {
                    var file = project.File($"{name}\\{fileName}.cs");
                    var type = file.Type(fileName, $"{fileName} of {name}");

                    type.Member("Save", $"stores a {fileName} in the {name} database");
                    type.Member("Load", $"{fileName}.Load");
                }
            }

            return solution.WithFakeEmbeddings();
        }

        private sealed class ShortVectorizer : IEmbeddingVectorizer
        {
            public string ModelName => "short";

            public string? ReportedModelName => null;

            public Task<IReadOnlyList<float[]>> VectorizeAsync(
                IReadOnlyList<string> texts,
                CancellationToken cancellationToken
                )
            {
                return Task.FromResult<IReadOnlyList<float[]>>(
                    new[] { new[] { 1f, 0f } }
                    );
            }
        }
    }
}
