using FreeAIr.Llm.Budget;
using Xunit;

namespace FreeAIr.Llm.Tests.Budget;

/// <summary>
/// How a request is counted without the model's tokenizer: a guess by characters, scaled by what
/// the server reported for the previous request, and left alone when the server reports nothing
/// believable.
/// </summary>
public sealed class TokenCounterFacts
{
    private const string Model = "test|model";

    [Fact]
    public void CyrillicCostsMoreThanLatinOfTheSameLength()
    {
        var latin = TokenEstimator.EstimateText(new string('a', 300));
        var cyrillic = TokenEstimator.EstimateText(new string('ж', 300));

        Assert.True(cyrillic > latin);
    }

    [Fact]
    public void NothingCostsNothingAndAMessageCostsItsOverhead()
    {
        Assert.Equal(0, TokenEstimator.EstimateText(null));
        Assert.Equal(0, TokenEstimator.EstimateText(string.Empty));
        Assert.Equal(
            TokenEstimator.PerMessageOverhead,
            TokenEstimator.EstimateMessage(LlmMessage.CreateUserMessage(string.Empty))
            );
    }

    [Fact]
    public void AToolResultIsCountedAsWellAsText()
    {
        var bare = TokenEstimator.EstimateMessage(
            LlmMessage.CreateToolResultMessage(new LlmToolResult("call_1", string.Empty))
            );
        var full = TokenEstimator.EstimateMessage(
            LlmMessage.CreateToolResultMessage(new LlmToolResult("call_1", new string('x', 1000)))
            );

        Assert.True(full > bare + 200);
    }

    [Fact]
    public void AnUncalibratedCounterReturnsTheGuessByCharacters()
    {
        var counter = new TokenCounter();

        Assert.False(counter.IsCalibrated);
        Assert.Equal(1000, counter.Scale(1000));
    }

    [Fact]
    public void AfterCalibrationThePreviousRequestCountsAsExactlyWhatTheServerSaid()
    {
        //this is the point of the ratio: the request just sent is the prefix of the next one, so
        //only the part added since is left to the guess
        var counter = new TokenCounter();

        Assert.True(counter.TryCalibrate(Model, 2000, 1500));

        Assert.True(counter.IsCalibrated);
        Assert.Equal(1500, counter.Scale(2000));
        Assert.Equal(1500 + 75, counter.Scale(2000 + 100));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-5)]
    public void AMissingOrZeroCountLeavesTheCalibrationAsItWas(
        int? reported
        )
    {
        var counter = new TokenCounter(0.8);

        Assert.False(counter.TryCalibrate(Model, 2000, reported));

        Assert.Equal(0.8, counter.Ratio);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1_000_000)]
    public void ACountWhichCannotBeThisRequestIsIgnored(
        int reported
        )
    {
        //a server reporting only the uncached part of the prompt, or something else entirely
        var counter = new TokenCounter();

        Assert.False(counter.TryCalibrate(Model, 2000, reported));

        Assert.False(counter.IsCalibrated);
        Assert.Equal(2000, counter.Scale(2000));
    }

    [Fact]
    public void AnImplausibleSavedRatioIsNotTrusted()
    {
        Assert.False(new TokenCounter(50.0).IsCalibrated);
        Assert.False(new TokenCounter(double.NaN).IsCalibrated);
        Assert.True(new TokenCounter(1.2).IsCalibrated);
    }

    [Fact]
    public void ANewChatStartsFromWhatTheLastChatOfTheModelLearned()
    {
        var key = "registry|" + Guid.NewGuid().ToString("N");
        Assert.Null(TokenCounter.GetLastRatio(key));

        new TokenCounter().TryCalibrate(key, 1000, 900);

        Assert.Equal(0.9, TokenCounter.GetLastRatio(key));
    }

    [Fact]
    public void TheBudgetLeavesRoomForTheAnswerAndTheError()
    {
        var uncalibrated = new TokenCounter();
        //a quarter of 16384 for the answer, 15% of the rest for the error of the guess
        Assert.Equal((int)(12288 * 0.85), uncalibrated.GetInputBudget(16384, null));
        //an explicit output limit is what is held back instead
        Assert.Equal((int)(15384 * 0.85), uncalibrated.GetInputBudget(16384, 1000));

        var calibrated = new TokenCounter(1.0);
        Assert.Equal((int)(12288 * 0.95), calibrated.GetInputBudget(16384, null));
    }

    [Fact]
    public void AnUnknownWindowNeverCutsAnything()
    {
        Assert.Equal(int.MaxValue, new TokenCounter().GetInputBudget(0, null));
    }

    [Fact]
    public void AnOutputLimitAsLargeAsTheWindowLeavesNoRoomAtAll()
    {
        Assert.Equal(0, new TokenCounter().GetInputBudget(8192, 8192));
    }
}
