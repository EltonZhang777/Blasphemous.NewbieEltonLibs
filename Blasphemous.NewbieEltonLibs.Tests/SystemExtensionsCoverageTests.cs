using Blasphemous.NewbieEltonLibs.Extensions.System;
using System;
using System.Collections.Generic;
using Xunit;

namespace Blasphemous.NewbieEltonLibs.Tests;

/// <summary>Verifies public .NET extension behavior.</summary>
public sealed class SystemExtensionsCoverageTests
{
    /// <summary>Verifies list movement in both directions and as a no-op.</summary>
    [Fact]
    public void MoveReordersItemsAtTheRequestedIndexes()
    {
        List<string> forward = ["a", "b", "c", "d"];
        forward.Move(1, 3);
        Assert.Equal(new[] { "a", "c", "d", "b" }, forward);

        List<string> backward = ["a", "b", "c", "d"];
        backward.Move(3, 1);
        Assert.Equal(new[] { "a", "d", "b", "c" }, backward);

        backward.Move(1, 1);
        Assert.Equal(new[] { "a", "d", "b", "c" }, backward);
    }

    /// <summary>Verifies list movement rejects invalid indexes.</summary>
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(2, 0)]
    [InlineData(0, 2)]
    public void MoveRejectsOutOfRangeIndexes(int oldIndex, int newIndex)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new List<int> { 1, 2 }.Move(oldIndex, newIndex));
    }

    /// <summary>Verifies enum movement wraps in both directions.</summary>
    [Fact]
    public void GetNextEnumValueWrapsAndSupportsNegativeSteps()
    {
        Assert.Equal(Direction.Second, Direction.First.GetNextEnumValue());
        Assert.Equal(Direction.First, Direction.Third.GetNextEnumValue());
        Assert.Equal(Direction.Third, Direction.First.GetNextEnumValue(-1));
        Assert.Equal(Direction.Second, Direction.First.GetNextEnumValue(4));
    }

    /// <summary>Verifies dictionary replacements are applied across a string.</summary>
    [Fact]
    public void ReplaceWordsUsesTheProvidedMapping()
    {
        string result = "red and blue".ReplaceWords(new Dictionary<string, string>
        {
            ["red"] = "green",
            ["blue"] = "yellow"
        });

        Assert.Equal("green and yellow", result);
    }

    private enum Direction
    {
        First,
        Second,
        Third
    }
}