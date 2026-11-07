using FreeAIr.Llm.Budget;
using Xunit;

namespace FreeAIr.Llm.Tests.Budget;

/// <summary>
/// How the batch features - natural language outlines and search - fill one request with files:
/// each file together with the files it refers to, each of them paid for once.
/// </summary>
public sealed class PortionBudgetFacts
{
    [Fact]
    public void FilesGoInUntilTheNextOneWouldNotFit()
    {
        var portion = new PortionBudget(100);

        Assert.True(portion.TryAdd(Part("a", 40), [], out _));
        Assert.True(portion.TryAdd(Part("b", 40), [], out _));
        Assert.False(portion.TryAdd(Part("c", 40), [], out _));

        Assert.Equal(80, portion.Used);
    }

    [Fact]
    public void AFileWhoseReferencesDoNotFitStartsTheNextPortion()
    {
        //the rejected file must leave the portion as it was, so that it can be retried whole
        var portion = new PortionBudget(100);
        portion.TryAdd(Part("a", 50), [], out _);

        Assert.False(portion.TryAdd(Part("b", 20), [Part("ref", 40)], out var accepted));

        Assert.Empty(accepted);
        Assert.Equal(50, portion.Used);
    }

    [Fact]
    public void AReferenceSharedByTwoFilesIsPaidForOnce()
    {
        var portion = new PortionBudget(100);

        Assert.True(portion.TryAdd(Part("a", 20), [Part("shared", 30)], out var first));
        Assert.True(portion.TryAdd(Part("b", 20), [Part("SHARED", 30)], out var second));

        Assert.Single(first);
        Assert.Empty(second);
        Assert.Equal(70, portion.Used);
    }

    [Fact]
    public void TheFirstFileGoesInEvenOverTheBudgetWithTheReferencesThatStillFit()
    {
        //there is nothing smaller to send than one file, so refusing it would lose it for good
        var portion = new PortionBudget(100);

        Assert.True(portion.TryAdd(Part("huge", 90), [Part("big ref", 50), Part("small ref", 5)], out var accepted));

        Assert.Equal("small ref", Assert.Single(accepted).Key);
        Assert.Equal(95, portion.Used);

        Assert.True(new PortionBudget(10).TryAdd(Part("enormous", 1000), [], out _));
    }

    [Fact]
    public void AFileWhichIsItsOwnReferenceIsCountedOnce()
    {
        var portion = new PortionBudget(100);

        Assert.True(portion.TryAdd(Part("a", 30), [Part("a", 30)], out var accepted));

        Assert.Empty(accepted);
        Assert.Equal(30, portion.Used);
    }

    [Fact]
    public void ClearStartsAnEmptyPortion()
    {
        var portion = new PortionBudget(100);
        portion.TryAdd(Part("a", 90), [], out _);

        portion.Clear();

        Assert.True(portion.IsEmpty);
        Assert.True(portion.TryAdd(Part("a", 90), [], out _));
    }

    private static PortionPart Part(
        string key,
        int tokens
        )
    {
        return new PortionPart(key, tokens);
    }
}
