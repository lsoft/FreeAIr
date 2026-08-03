using FreeAIr.Embedding;
using FreeAIr.NLOutline.Tree;

namespace FreeAIr.Rag.Tests
{
    /// <summary>
    /// An <see cref="IEmbeddingVectorizer"/> which answers from a table instead of from a server.
    /// Anything not in the table gets a vector of its own, orthogonal to everything else, so that a
    /// test which forgot to arrange a text still gets a well defined "matches nothing" rather than
    /// an accidental hit.
    /// </summary>
    internal sealed class FakeVectorizer : IEmbeddingVectorizer
    {
        private readonly Dictionary<string, float[]> _byText;
        private readonly int _dimensions;

        public string ModelName => "fake-embedding-model";

        /// <summary>
        /// Deliberately not <see cref="ModelName"/>: the two differ in real life, which is the
        /// whole reason the index stores both.
        /// </summary>
        public string? ReportedModelName
        {
            get;
            set;
        } = "fake-embedding-model-q8";

        /// <summary>Every batch this vectorizer has been asked for, in order.</summary>
        public List<IReadOnlyList<string>> Requests
        {
            get;
        } = new();

        public FakeVectorizer(
            int dimensions,
            Dictionary<string, float[]>? byText = null
            )
        {
            _dimensions = dimensions;
            _byText = byText ?? new Dictionary<string, float[]>(StringComparer.Ordinal);
        }

        public FakeVectorizer Add(
            string text,
            params float[] vector
            )
        {
            _byText[text] = vector;
            return this;
        }

        public Task<IReadOnlyList<float[]>> VectorizeAsync(
            IReadOnlyList<string> texts,
            CancellationToken cancellationToken
            )
        {
            cancellationToken.ThrowIfCancellationRequested();

            Requests.Add(texts.ToArray());

            var result = new List<float[]>(texts.Count);
            foreach (var text in texts)
            {
                result.Add(
                    _byText.TryGetValue(text, out var vector)
                        ? vector
                        : Orthogonal(text)
                    );
            }

            return Task.FromResult<IReadOnlyList<float[]>>(result);
        }

        /// <summary>
        /// Stands for "a different model of the same size": two vectorizers with different variants
        /// answer the same text with different vectors, which is what the fingerprint has to catch
        /// and what the length of the vector cannot.
        /// </summary>
        public int Variant
        {
            get;
            set;
        }

        /// <summary>A basis vector picked by the hash of the text, i.e. unrelated to everything.</summary>
        private float[] Orthogonal(
            string text
            )
        {
            var result = new float[_dimensions];
            var index = Math.Abs(text.GetHashCode() + Variant) % _dimensions;
            result[index] = 1f;
            return result;
        }
    }

    /// <summary>
    /// Builds outline trees the way the scanners do, so that a test can say what it means rather
    /// than assemble nodes by hand.
    /// </summary>
    internal static class TreeFactory
    {
        public static OutlineNode Solution(
            string name = "Test.sln"
            )
        {
            return new OutlineNode(
                OutlineKindEnum.Solution,
                string.Empty,
                name,
                string.Empty,
                null,
                new List<OutlineNode>()
                );
        }

        public static OutlineNode Project(
            this OutlineNode solution,
            string relativePath,
            string name
            )
        {
            return solution.AddChild(OutlineKindEnum.Project, relativePath, name, string.Empty);
        }

        public static OutlineNode File(
            this OutlineNode project,
            string relativePath
            )
        {
            return project.AddChild(OutlineKindEnum.File, relativePath, relativePath, string.Empty);
        }

        public static OutlineNode Type(
            this OutlineNode file,
            string name,
            string outlineText
            )
        {
            return file.AddChild(
                OutlineKindEnum.ClassOrSimilarEntity,
                file.RelativePath,
                name,
                outlineText
                );
        }

        public static OutlineNode Member(
            this OutlineNode type,
            string name,
            string outlineText
            )
        {
            return type.AddChild(
                OutlineKindEnum.MethodOfClassOrSimilarPart,
                type.RelativePath,
                type.Target + "." + name,
                outlineText
                );
        }

        /// <summary>
        /// Gives every node which is supposed to have one a vector, without going near a model.
        /// The vector is derived from the outline text, so two runs of the same tree agree.
        /// </summary>
        public static OutlineNode WithFakeEmbeddings(
            this OutlineNode root,
            int dimensions = 8
            )
        {
            foreach (var node in OutlineEmbedder.SelectNodesToEmbed(root))
            {
                node.AddEmbedding(FakeVector(node.OutlineText, dimensions));
            }

            return root;
        }

        public static float[] FakeVector(
            string text,
            int dimensions = 8
            )
        {
            var result = new float[dimensions];
            var seed = 0;
            foreach (var c in text)
            {
                seed = unchecked(seed * 31 + c);
            }

            var random = new Random(seed);
            for (var i = 0; i < dimensions; i++)
            {
                result[i] = (float)(random.NextDouble() * 2d - 1d);
            }

            return result;
        }

        public static List<OutlineNode> Flatten(
            this OutlineNode root
            )
        {
            var result = new List<OutlineNode>();
            root.ApplyRecursive(node => result.Add(node));
            return result;
        }
    }

    /// <summary>A temporary folder which deletes itself with the test.</summary>
    internal sealed class TempFolder : IDisposable
    {
        public string Path
        {
            get;
        }

        public string IndexFilePath => System.IO.Path.Combine(Path, "Test_embeddings.json");

        public TempFolder()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "freeair-rag-tests",
                Guid.NewGuid().ToString("N")
                );
            Directory.CreateDirectory(Path);
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch (IOException)
            {
                //a leftover temp folder is not worth failing a green test over
            }
        }
    }
}
