using FreeAIr.Llm.Streaming;

namespace FreeAIr.Llm.Tests.Fakes;

/// <summary>
/// Drains a transport's stream into the three things a test asks about: the answer text, the tool
/// calls the accumulator rebuilt out of the fragments, and how the turn ended.
///
/// It does what <c>LLMReader</c> does in the product, which is deliberate: a test that assembled
/// the stream some other way would not be testing what the chat actually sees.
/// </summary>
public static class StreamEventCollector
{
    public sealed record Result(
        string Text,
        IReadOnlyList<LlmToolCall> ToolCalls,
        LlmFinishReason FinishReason,
        IReadOnlyList<string> Faults,
        IReadOnlyList<LlmStreamEvent> Events
        );

    public static async Task<Result> CollectAsync(
        ILlmTransport transport,
        LlmRequest request,
        CancellationToken cancellationToken = default
        )
    {
        var text = new System.Text.StringBuilder();
        var accumulator = new ToolCallAccumulator();
        var finishReason = LlmFinishReason.Unknown;
        var faults = new List<string>();
        var events = new List<LlmStreamEvent>();

        await foreach (var streamEvent in transport.StreamAsync(request, cancellationToken))
        {
            events.Add(streamEvent);
            accumulator.Append(streamEvent);

            switch (streamEvent)
            {
                case LlmTextDeltaEvent delta:
                    text.Append(delta.Text);
                    break;

                case LlmFinishedEvent finished:
                    finishReason = finished.Reason;
                    break;

                case LlmProtocolFaultEvent fault:
                    faults.Add(fault.Message);
                    break;
            }
        }

        return new Result(
            text.ToString(),
            accumulator.Build(),
            finishReason,
            faults,
            events
            );
    }
}
