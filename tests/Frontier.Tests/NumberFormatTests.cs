using IdleBar.Ui;
using Xunit;

namespace IdleBar.Tests;

public sealed class NumberFormatTests
{
    [Theory]
    [InlineData(999.9, "999")]
    [InlineData(1_240, "1,24 k")]
    [InlineData(56_700_000, "56,7 M")]
    [InlineData(3e9, "3,00 Md")]
    [InlineData(4.5e21, "4,50 Td")]
    [InlineData(7e33, "7,00 Qtd")]
    [InlineData(1.5e48, "1,50e48")]
    public void LargeAmountsKeepAShortForm(double value, string expected) => Assert.Equal(expected, NumberFormat.Amount(value));

    [Fact]
    public void GoldIsInvariable() => Assert.Equal("1 or", NumberFormat.Gold(1));
}
