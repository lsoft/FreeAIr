namespace FreeAIr.Llm.Tests.Fakes;

/// <summary>
/// Attaches a sink to <see cref="LlmDiagnostics"/> for the duration of one test and puts back
/// whatever was there before.
///
/// The sink is a static, and these tests are the only thing which reads it, so the assembly runs
/// its collections one at a time (see <c>TestParallelization.cs</c>) rather than have one test's
/// reports land in another's list.
/// </summary>
public sealed class CapturedDiagnostics : IDisposable
{
    private readonly Action<string>? _previous;

    /// <summary>Everything reported since this capture started, in order.</summary>
    public List<string> Reports
    {
        get;
    } = new();

    public CapturedDiagnostics()
    {
        _previous = LlmDiagnostics.Sink;
        LlmDiagnostics.Sink = Reports.Add;
    }

    /// <summary>Whether anything reported mentions the given text, case-insensitively.</summary>
    public bool Mentions(
        string text
        )
    {
        return Reports.Exists(r => r.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The reports joined, for an assertion message which says what was actually logged.</summary>
    public override string ToString()
    {
        return Reports.Count == 0
            ? "<nothing was reported>"
            : string.Join(Environment.NewLine, Reports);
    }

    public void Dispose()
    {
        LlmDiagnostics.Sink = _previous;
    }
}
