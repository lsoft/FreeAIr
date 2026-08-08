using FreeAIr.Embedding;
using FreeAIr.Embedding.Json;
using FreeAIr.Find;
using FreeAIr.NLOutline.Tree;
using Xunit;
using Xunit.Abstractions;

namespace FreeAIr.Search.Tests
{
    /// <summary>
    /// The whole point of the calibration, checked against a real model: a threshold measured on
    /// the index has to let the answers through and keep the nonsense out, without anybody having
    /// picked a number.
    ///
    /// The corpus is written here rather than taken from a solution on disk, so that the numbers
    /// this test reports mean the same thing on any machine. Skipped unless
    /// <see cref="IntegrationEnvironment.EndpointVariableName"/> names a server.
    /// </summary>
    public sealed class RagCalibrationIntegrationFacts
    {
        private readonly ITestOutputHelper _output;

        public RagCalibrationIntegrationFacts(
            ITestOutputHelper output
            )
        {
            _output = output;
        }

        /// <summary>A small imaginary solution: file, type, outline.</summary>
        private static readonly (string Path, string Target, string Outline)[] _corpus =
        {
            ("Data\\VoyageRepository.cs", "VoyageRepository", "Reads voyages and their stations from the database."),
            ("Data\\Connection.cs", "Connection", "Opens the connection to the database and keeps the pool of them."),
            ("Data\\Migration.cs", "Migration", "Upgrades the database schema to the current version on startup."),
            ("Logic\\TransferSearcher.cs", "TransferSearcher", "Searches for voyages which match the given date and conditions."),
            ("Logic\\RouteBuilder.cs", "RouteBuilder", "Builds a route out of the voyages found, station by station."),
            ("Logic\\PriceCalculator.cs", "PriceCalculator", "Calculates the price of a route including the discounts of the passenger."),
            ("Ui\\MainWindow.xaml.cs", "MainWindow", "The main window of the application and the layout of its panels."),
            ("Ui\\RouteView.xaml.cs", "RouteView", "Draws the found route on the screen as a list of legs."),
            ("Ui\\Theme.cs", "Theme", "Colours and fonts of the user interface, switched between light and dark."),
            ("Infrastructure\\Logger.cs", "Logger", "Writes the diagnostic log into a rolling file."),
            ("Infrastructure\\Settings.cs", "Settings", "Reads the settings of the application from a json file."),
            ("Infrastructure\\Scheduler.cs", "Scheduler", "Runs background jobs on a timer and reports what they did."),
        };

        /// <summary>Questions this imaginary solution can answer, with the file which has to win.</summary>
        private static readonly (string Query, string Path)[] _answerable =
        {
            ("how are voyages searched by date", "Logic\\TransferSearcher.cs"),
            ("where is the database connection opened", "Data\\Connection.cs"),
            ("how is the price of a trip calculated", "Logic\\PriceCalculator.cs"),
            ("где приложение читает свои настройки", "Infrastructure\\Settings.cs"),
        };

        /// <summary>Questions it cannot answer, of the plausible kind rather than the absurd kind.</summary>
        private static readonly string[] _hopeless =
        {
            "how is the oauth authentication configured",
            "which class sends the email notifications",
            "как настроить деплой в kubernetes",
            "where are the unit tests of the payment gateway",
        };

        [IntegrationFact]
        public async Task A_measured_threshold_separates_the_answers_from_the_nonsense()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));

            var vectorizer = new OpenAIEmbeddingVectorizer(
                await IntegrationEnvironment.ResolveModelAsync(timeout.Token),
                IntegrationEnvironment.RequiredEndpoint,
                IntegrationEnvironment.Token
                );

            var json = await BuildIndexJsonAsync(vectorizer, timeout.Token);

            var calibration = await RagCalibrator.RunAsync(
                ToIndex(json, null),
                vectorizer,
                new RagCalibrationProbes(irrelevant: _hopeless),
                timeout.Token
                );

            Assert.NotNull(calibration);

            //this is what an index carries after a build: the numbers travel inside it, which is
            //what lets the search have a threshold without anybody configuring one
            var index = ToIndex(json, calibration);

            var threshold = calibration!.ComputeThreshold(0.2d);

            _output.WriteLine($"model      : {vectorizer.ReportedModelName ?? vectorizer.ModelName}");
            _output.WriteLine($"dimensions : {index.Dimensions}");
            _output.WriteLine($"noise      : {calibration.NoiseCeiling:F3}");
            _output.WriteLine($"threshold  : {threshold:F3} (sensitivity 0.2)");
            _output.WriteLine(string.Empty);

            var options = new RagShortlistOptions { Sensitivity = 0.2d };

            foreach (var (query, expected) in _answerable)
            {
                var shortlist = await RagShortlist.BuildAsync(index, vectorizer, query, options, timeout.Token);

                var best = shortlist.Candidates.Count == 0
                    ? null
                    : shortlist.Candidates[0]
                    ;

                _output.WriteLine(
                    $"[answerable] {query}"
                    + Environment.NewLine
                    + $"      -> {best?.RelativePath ?? "<nothing>"} {best?.Score ?? 0f:F3}, {shortlist.Candidates.Count} file(s) passed"
                    );

                Assert.NotNull(best);
                Assert.Equal(expected, best!.RelativePath);
            }

            _output.WriteLine(string.Empty);

            foreach (var query in _hopeless)
            {
                var shortlist = await RagShortlist.BuildAsync(index, vectorizer, query, options, timeout.Token);

                _output.WriteLine(
                    $"[hopeless  ] {query}"
                    + Environment.NewLine
                    + $"      -> {shortlist.Candidates.Count} file(s) passed"
                    );

                //the queries the threshold was measured on are the ones it has to refuse
                Assert.Empty(shortlist.Candidates);
            }
        }

        [IntegrationFact]
        public async Task The_fingerprint_of_one_model_is_the_same_on_every_build()
        {
            //two rebuilds of the same index must not disagree about their own model, or every
            //search would be refused
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));

            var vectorizer = new OpenAIEmbeddingVectorizer(
                await IntegrationEnvironment.ResolveModelAsync(timeout.Token),
                IntegrationEnvironment.RequiredEndpoint,
                IntegrationEnvironment.Token
                );

            var first = await EmbeddingSpaceFingerprint.BuildAsync(vectorizer, timeout.Token);
            var second = await EmbeddingSpaceFingerprint.BuildAsync(vectorizer, timeout.Token);

            Assert.NotNull(first);
            Assert.NotNull(second);

            //identical bytes, i.e. no diff in the committed metadata file after a rebuild
            Assert.Equal(first!.Encode(), second!.Encode());

            var restored = EmbeddingSpaceFingerprint.FromEncoded(first.Encode());
            Assert.NotNull(restored);

            var similarity = restored!.SimilarityTo(second.Vectors);
            _output.WriteLine($"stored vs freshly measured: {similarity:F5}");

            Assert.True(
                similarity >= EmbeddingSpaceFingerprint.SameSpaceThreshold,
                $"the model does not agree with itself across two calls ({similarity:F5})"
                );
        }

        private static async Task<EmbeddingOutlineJsonObject> BuildIndexJsonAsync(
            IEmbeddingVectorizer vectorizer,
            CancellationToken cancellationToken
            )
        {
            var root = TreeFactory.Solution("Imaginary.sln");
            var byProject = new Dictionary<string, OutlineNode>(StringComparer.OrdinalIgnoreCase);

            foreach (var (path, target, outline) in _corpus)
            {
                var projectName = path.Substring(0, path.IndexOf('\\'));

                if (!byProject.TryGetValue(projectName, out var project))
                {
                    project = root.Project($"{projectName}\\{projectName}.csproj", projectName);
                    byProject.Add(projectName, project);
                }

                project.File(path).Type(target, outline);
            }

            await new OutlineEmbedder(vectorizer).GenerateEmbeddingsAsync(root, cancellationToken);

            return new EmbeddingOutlineJsonObject(root, "test-agent", vectorizer.ModelName, "test-endpoint");
        }

        private static EmbeddingIndex ToIndex(
            EmbeddingOutlineJsonObject json,
            EmbeddingCalibration? calibration
            )
        {
            return EmbeddingIndex.Build(
                new EmbeddingIndexMetadata(
                    "imaginary.json",
                    new DateTime(2026, 1, 1),
                    "test-agent",
                    json.EmbeddingModel,
                    json.EmbeddingDimensions,
                    json.ReportedEmbeddingModel,
                    null,
                    calibration
                    ),
                json.Outlines!.Outlines,
                json.Embeddings!.Embeddings
                );
        }
    }
}
