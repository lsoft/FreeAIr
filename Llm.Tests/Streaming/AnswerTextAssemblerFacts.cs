using FreeAIr.Llm.Streaming;
using System.Text;
using Xunit;

namespace FreeAIr.Llm.Tests.Streaming;

/// <summary>
/// How the reasoning of a thinking model becomes the text of an answer.
///
/// This is what the chat window is handed, so everything the user sees of the feature is decided
/// here: a think block is opened on the first fragment, closed by the first thing which is not
/// reasoning, and never left open — an answer holding an unclosed tag swallows every word appended
/// after it, which is the failure worth a test of its own.
/// </summary>
public sealed class AnswerTextAssemblerFacts
{
    [Fact]
    public void ReasoningIsWrappedInAThinkBlockAndTheAnswerFollowsIt()
    {
        var text = Assemble(
            showReasoning: true,
            new LlmReasoningDeltaEvent("I should "),
            new LlmReasoningDeltaEvent("answer 42."),
            new LlmTextDeltaEvent("42"),
            new LlmFinishedEvent(LlmFinishReason.Stop)
            );

        Assert.Equal(
            "<think>" + Environment.NewLine
            + "I should answer 42." + Environment.NewLine
            + "</think>" + Environment.NewLine + Environment.NewLine
            + "42",
            text
            );
    }

    [Fact]
    public void WithoutTheSettingTheReasoningIsNotInTheAnswerAtAll()
    {
        //hiding it would still keep it, and what is kept is sent back to the model as history on
        //every later turn of the chat
        var text = Assemble(
            showReasoning: false,
            new LlmReasoningDeltaEvent("pages and pages"),
            new LlmTextDeltaEvent("42"),
            new LlmFinishedEvent(LlmFinishReason.Stop)
            );

        Assert.Equal("42", text);
    }

    [Fact]
    public void AnAnswerWithoutReasoningIsUntouched()
    {
        var text = Assemble(
            showReasoning: true,
            new LlmTextDeltaEvent("Hello"),
            new LlmTextDeltaEvent(", world"),
            new LlmFinishedEvent(LlmFinishReason.Stop)
            );

        Assert.Equal("Hello, world", text);
    }

    [Fact]
    public void ReasoningWhichEndsTheTurnIsClosedByTheFlush()
    {
        //a turn ending in a tool call says nothing, so nothing but the end of the stream is left to
        //close the block
        var assembler = new AnswerTextAssembler(showReasoning: true);

        var opened = assembler.Append(new LlmReasoningDeltaEvent("I should build."));
        var closed = assembler.Flush();

        Assert.Equal("<think>" + Environment.NewLine + "I should build.", opened);
        Assert.Equal(Environment.NewLine + "</think>" + Environment.NewLine + Environment.NewLine, closed);
        Assert.Equal(string.Empty, assembler.Flush());
    }

    [Fact]
    public void AToolCallClosesTheBlockBeforeTheAnswerResumes()
    {
        //an interleaved-thinking model reasons, calls a tool and reasons again in one turn, and a
        //single block around the lot would hide the answer written in between
        var text = Assemble(
            showReasoning: true,
            new LlmReasoningDeltaEvent("first"),
            new LlmToolCallOpenedEvent(0, "call_1", "VS.Build"),
            new LlmTextDeltaEvent("building"),
            new LlmReasoningDeltaEvent("second"),
            new LlmTextDeltaEvent("done"),
            new LlmFinishedEvent(LlmFinishReason.Stop)
            );

        Assert.Equal(2, CountOf(text, AnswerTextAssembler.ThinkStart));
        Assert.Equal(2, CountOf(text, AnswerTextAssembler.ThinkEnd));
        Assert.Contains("building", text);
        Assert.EndsWith("done", text);
    }

    [Fact]
    public void TheHistoryOfTheNextRequestCarriesTheAnswerWithoutTheReasoning()
    {
        var answer = Assemble(
            showReasoning: true,
            new LlmReasoningDeltaEvent("deliberation"),
            new LlmTextDeltaEvent("42"),
            new LlmFinishedEvent(LlmFinishReason.Stop)
            );

        Assert.Equal("42", AnswerTextAssembler.WithoutReasoning(answer));
    }

    [Fact]
    public void EveryThinkBlockIsRemovedFromTheHistoryAndNotJustTheFirst()
    {
        //a model which writes `<think>` into its own text - DeepSeek-R1 under llama.cpp does - can
        //write several, and the ones after the first are the ones a leading-block strip misses
        var answer =
            "<think>one</think>alpha<think>two</think>beta";

        Assert.Equal("alphabeta", AnswerTextAssembler.WithoutReasoning(answer));
    }

    [Fact]
    public void AnAnswerStillStreamingKeepsItsUnclosedBlock()
    {
        //the history is built from a chat whose last answer may still be arriving, and cutting
        //everything after an unclosed tag would take the answer with it
        var answer = "<think>still going";

        Assert.Equal(answer, AnswerTextAssembler.WithoutReasoning(answer));
    }

    [Fact]
    public void AnAnswerWhichNeverReasonedIsNotRewritten()
    {
        const string answer = "plain answer";

        Assert.Same(answer, AnswerTextAssembler.WithoutReasoning(answer));
    }

    /// <summary>Runs the events through an assembler the way the reader does, and returns the answer they built.</summary>
    private static string Assemble(
        bool showReasoning,
        params LlmStreamEvent[] events
        )
    {
        var assembler = new AnswerTextAssembler(showReasoning);
        var answer = new StringBuilder();

        foreach (var streamEvent in events)
        {
            answer.Append(assembler.Append(streamEvent));
        }

        answer.Append(assembler.Flush());

        return answer.ToString();
    }

    private static int CountOf(
        string text,
        string what
        )
    {
        var count = 0;
        for (var i = text.IndexOf(what, StringComparison.Ordinal); i >= 0; i = text.IndexOf(what, i + what.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }
}
