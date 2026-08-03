using FreeAIr.Embedding.Json;
using FreeAIr.NLOutline.Json;
using FreeAIr.NLOutline.Tree;
using Xunit;

namespace FreeAIr.Search.Tests
{
    /// <summary>
    /// The shape of the index used to be a fourth file. These are the facts which let it go away.
    /// </summary>
    public sealed class OutlineTreeAssemblerFacts
    {
        [Fact]
        public void The_tree_survives_a_trip_through_the_flat_file()
        {
            var original = IndexFileFacts.SampleTree();

            var assembled = Assemble(original);

            Assert.NotNull(assembled);
            Assert.Equal(
                Describe(original),
                Describe(assembled!)
                );
        }

        [Fact]
        public void A_member_comes_back_under_its_own_type()
        {
            var solution = TreeFactory.Solution();
            var file = solution.Project("A\\A.csproj", "A").File("A\\Two.cs");
            var first = file.Type("First", "the first type");
            var second = file.Type("Second", "the second type");
            first.Member("Save", "saves the first");
            second.Member("Save", "saves the second");

            var assembled = Assemble(solution)!;

            var assembledFile = Find(assembled, OutlineKindEnum.File, "A\\Two.cs");
            var assembledFirst = assembledFile.Children.Single(c => c.Target == "First");
            var assembledSecond = assembledFile.Children.Single(c => c.Target == "Second");

            Assert.Equal("First.Save", assembledFirst.Children.Single().Target);
            Assert.Equal("Second.Save", assembledSecond.Children.Single().Target);
        }

        [Fact]
        public void A_file_comes_back_under_the_project_whose_folder_holds_it()
        {
            var solution = TreeFactory.Solution();
            var outer = solution.Project("Outer\\Outer.csproj", "Outer");
            var inner = solution.Project("Outer\\Inner\\Inner.csproj", "Inner");
            outer.File("Outer\\Thing.cs").Type("Thing", "a thing");
            inner.File("Outer\\Inner\\Gadget.cs").Type("Gadget", "a gadget");

            var assembled = Assemble(solution)!;

            var assembledOuter = Find(assembled, OutlineKindEnum.Project, "Outer\\Outer.csproj");
            var assembledInner = Find(assembled, OutlineKindEnum.Project, "Outer\\Inner\\Inner.csproj");

            //the nested project takes its own file, the outer one does not swallow it
            Assert.Equal("Outer\\Thing.cs", assembledOuter.Children.Single().RelativePath);
            Assert.Equal("Outer\\Inner\\Gadget.cs", assembledInner.Children.Single().RelativePath);
        }

        [Fact]
        public void A_member_whose_type_is_gone_is_kept_on_the_file()
        {
            //losing a node would silently shrink the index, which is the one failure the user
            //cannot see
            var outlines = new List<OutlineItselfJsonObject>
            {
                Outline(OutlineKindEnum.Solution, string.Empty, "Test.sln"),
                Outline(OutlineKindEnum.Project, "A\\A.csproj", "A"),
                Outline(OutlineKindEnum.File, "A\\Thing.cs", "A\\Thing.cs"),
                Outline(OutlineKindEnum.MethodOfClassOrSimilarPart, "A\\Thing.cs", "Vanished.Save"),
            };

            var assembled = OutlineTreeAssembler.Assemble(outlines)!;

            var file = Find(assembled, OutlineKindEnum.File, "A\\Thing.cs");
            Assert.Equal("Vanished.Save", file.Children.Single().Target);
        }

        [Fact]
        public void Without_a_solution_node_there_is_no_tree()
        {
            var outlines = new List<OutlineItselfJsonObject>
            {
                Outline(OutlineKindEnum.Project, "A\\A.csproj", "A"),
            };

            Assert.Null(OutlineTreeAssembler.Assemble(outlines));
        }

        [Fact]
        public void The_vectors_land_on_the_nodes_they_belong_to()
        {
            var original = IndexFileFacts.SampleTree();

            var json = new EmbeddingOutlineJsonObject(original);
            var assembled = json.BuildOutlineTree()!;

            var expected = original
                .Flatten()
                .Where(n => n.Embedding is not null)
                .ToDictionary(n => n.Id, n => n.Embedding!);

            var actual = assembled
                .Flatten()
                .Where(n => n.Embedding is not null)
                .ToDictionary(n => n.Id, n => n.Embedding!);

            Assert.Equal(expected.Keys.OrderBy(k => k), actual.Keys.OrderBy(k => k));

            foreach (var pair in expected)
            {
                Assert.Equal(pair.Value, actual[pair.Key]);
            }
        }

        [Fact]
        public void A_reassembled_tree_writes_the_same_file_it_came_from()
        {
            //this is what makes an incremental rebuild safe: the nodes reused from the old index
            //serialize exactly as they did before
            var original = IndexFileFacts.SampleTree();

            var first = new OutlinesItselfJsonObject(original);
            var assembled = OutlineTreeAssembler.Assemble(first.Outlines)!;
            var second = new OutlinesItselfJsonObject(assembled);

            Assert.Equal(
                first.Outlines.Select(Describe),
                second.Outlines.Select(Describe)
                );
        }

        private static OutlineNode? Assemble(
            OutlineNode root
            )
        {
            return OutlineTreeAssembler.Assemble(
                new OutlinesItselfJsonObject(root).Outlines
                );
        }

        private static OutlineNode Find(
            OutlineNode root,
            OutlineKindEnum kind,
            string relativePath
            )
        {
            return root
                .Flatten()
                .Single(n => n.Kind == kind && n.RelativePath == relativePath);
        }

        private static OutlineItselfJsonObject Outline(
            OutlineKindEnum kind,
            string relativePath,
            string target
            )
        {
            return new OutlineItselfJsonObject
            {
                Id = OutlineNode.GenerateGuid(kind, target, relativePath),
                Kind = kind,
                RelativePath = relativePath,
                Target = target,
                OutlineText = target
            };
        }

        private static string Describe(
            OutlineItselfJsonObject outline
            )
        {
            return $"{outline.Kind}|{outline.RelativePath}|{outline.Target}|{outline.Id}";
        }

        /// <summary>
        /// The tree as text, indented by depth, so that a difference in the shape shows up as a
        /// readable diff rather than as a boolean.
        /// </summary>
        private static string Describe(
            OutlineNode root
            )
        {
            var builder = new System.Text.StringBuilder();

            void Walk(OutlineNode node, int depth)
            {
                builder
                    .Append(new string(' ', depth * 2))
                    .Append(node.Kind)
                    .Append('|')
                    .Append(node.RelativePath)
                    .Append('|')
                    .Append(node.Target)
                    .AppendLine();

                foreach (var child in node.Children)
                {
                    Walk(child, depth + 1);
                }
            }

            Walk(root, 0);

            return builder.ToString();
        }
    }
}
