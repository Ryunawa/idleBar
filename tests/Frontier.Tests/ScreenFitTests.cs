using System;
using IdleBar.Desktop;
using Xunit;

namespace IdleBar.Tests;

public sealed class ScreenFitTests
{
    [Theory]
    [InlineData(1920, 1080, 1f, 1f)]
    [InlineData(2560, 1440, 1f, 1.3333f)]
    [InlineData(3840, 2160, 1.5f, 2f)]
    [InlineData(3440, 1440, 1f, 1.3333f)]
    [InlineData(1366, 768, 1f, 1f)]
    [InlineData(1920, 1080, 1.25f, 1.25f)]
    [InlineData(1080, 1920, 1f, 1f)]
    public void TheBarGrowsWithTheScreen(int width, int height, float dpiScale, float expected) =>
        Assert.Equal(expected, ScreenFit.Scale(width, height, dpiScale), 3);

    [Fact]
    public void TheBarKeepsTheSameShareOfTwoScreens()
    {
        float small = MathF.Round(56 * ScreenFit.Scale(1920, 1080, 1f)) / 1080f;
        float large = MathF.Round(56 * ScreenFit.Scale(2560, 1440, 1f)) / 1440f;
        Assert.Equal(small, large, 2);
    }
}
