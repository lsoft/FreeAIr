using FreeAIr.Llm.Budget;
using Xunit;

namespace FreeAIr.Llm.Tests.Budget;

/// <summary>
/// What is sent when the chat no longer fits the window: the system prompt, the tools and the
/// turn being answered always, and of the earlier turns the most recent ones that still fit.
/// </summary>
public sealed class ContextWindowFitterFacts
{
    [Fact]
    public void EverythingIsSentWhenItFits()
    {
        var fit = Fit(
            budget: 100_000,
            Turn("first"),
            Turn("second")
            );

        Assert.Equal(2, fit.KeptTurnCount);
        Assert.Equal(0, fit.DroppedTurnCount);
        Assert.Equal(
            new[] { "first", "first answer", "second", "second answer", "now" },
            fit.Messages.Select(m => m.Text).ToArray()
            );
        Assert.False(fit.IsOverBudget);
    }

    [Fact]
    public void TheOldestTurnsAreLeftOutFirst()
    {
        var turns = new[] { Turn("oldest", 400), Turn("middle", 400), Turn("newest", 400) };
        var current = Current();

        //room for the current turn and exactly two of the earlier ones
        var budget = Raw(current) + Raw(turns[1]) + Raw(turns[2]);

        var fit = ContextWindowFitter.Fit(null, [], turns, current, budget, new TokenCounter());

        Assert.Equal(2, fit.KeptTurnCount);
        Assert.Equal(1, fit.DroppedTurnCount);
        Assert.Equal("middle", fit.Messages[0].Text);
        Assert.Equal("now", fit.Messages[^1].Text);
        Assert.True(fit.EstimatedTokens <= budget);
    }

    [Fact]
    public void AnOlderTurnIsNeverSentInPlaceOfANewerOneWhichDoesNotFit()
    {
        //a small old turn would fit where the big newer one did not, but the model would then see
        //a dialogue with a hole in the middle
        var turns = new[] { Turn("small old", 10), Turn("big", 5000), Turn("recent", 10) };
        var current = Current();

        var budget = Raw(current) + Raw(turns[2]) + Raw(turns[0]);

        var fit = ContextWindowFitter.Fit(null, [], turns, current, budget, new TokenCounter());

        Assert.Equal(1, fit.KeptTurnCount);
        Assert.Equal("recent", fit.Messages[0].Text);
    }

    [Fact]
    public void TheCurrentTurnAndTheSystemPromptGoEvenOverTheBudget()
    {
        var fit = ContextWindowFitter.Fit(
            "You are a helpful assistant.",
            [],
            [Turn("old")],
            Current(),
            inputBudget: 1,
            new TokenCounter()
            );

        Assert.True(fit.IsOverBudget);
        Assert.Equal(0, fit.KeptTurnCount);
        Assert.Equal("now", Assert.Single(fit.Messages).Text);
    }

    [Fact]
    public void ToolDefinitionsArePartOfTheFixedCost()
    {
        var turns = new[] { Turn("old", 100) };
        var current = Current();
        var tool = new LlmToolDefinition("VS.Search", "Searches.", new string('x', 4000));

        var budget = Raw(current) + Raw(turns[0]);

        var withoutTools = ContextWindowFitter.Fit(null, [], turns, current, budget, new TokenCounter());
        var withTools = ContextWindowFitter.Fit(null, [tool], turns, current, budget, new TokenCounter());

        Assert.Equal(1, withoutTools.KeptTurnCount);
        Assert.Equal(0, withTools.KeptTurnCount);
    }

    [Fact]
    public void TheRawEstimateCoversEverythingSentSoTheServerCountCanCalibrateIt()
    {
        var turns = new[] { Turn("a", 50), Turn("b", 50) };
        var current = Current();

        var fit = ContextWindowFitter.Fit("system", [], turns, current, 100_000, new TokenCounter());

        Assert.Equal(
            TokenEstimator.EstimatePreamble("system", []) + TokenEstimator.EstimateMessages(fit.Messages),
            fit.RawEstimate
            );
    }

    [Fact]
    public void ACalibratedCounterKeepsMoreHistoryWhenTheModelCountsLess()
    {
        var turns = Enumerable.Range(0, 10).Select(i => Turn("turn " + i, 300)).ToArray();
        var current = Current();
        var budget = Raw(current) + 3 * Raw(turns[0]);

        var guessing = ContextWindowFitter.Fit(null, [], turns, current, budget, new TokenCounter());
        var calibrated = ContextWindowFitter.Fit(null, [], turns, current, budget, new TokenCounter(0.5));

        Assert.True(calibrated.KeptTurnCount > guessing.KeptTurnCount);
    }

    private static ContextWindowFit Fit(
        int budget,
        params IReadOnlyList<LlmMessage>[] turns
        )
    {
        return ContextWindowFitter.Fit(null, [], turns, Current(), budget, new TokenCounter());
    }

    private static IReadOnlyList<LlmMessage> Turn(
        string prompt,
        int answerLength = 0
        )
    {
        return
        [
            LlmMessage.CreateUserMessage(prompt),
            LlmMessage.CreateAssistantMessage(answerLength == 0 ? prompt + " answer" : new string('a', answerLength)),
        ];
    }

    private static IReadOnlyList<LlmMessage> Current() => [LlmMessage.CreateUserMessage("now")];

    private static int Raw(
        IReadOnlyList<LlmMessage> messages
        )
    {
        return TokenEstimator.EstimateMessages(messages);
    }
}
