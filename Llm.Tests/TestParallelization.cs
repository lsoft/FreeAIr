using Xunit;

// LlmDiagnostics.Sink is a static, and the tests which capture it would otherwise collect the
// reports of whatever else is running at the same time. The whole suite takes well under a second,
// so running the collections one at a time costs nothing worth measuring.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
