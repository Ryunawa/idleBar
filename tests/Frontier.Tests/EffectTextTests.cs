using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class EffectTextTests
{
    [Theory]
    [InlineData(BuildingKind.Forge, 30, "+30 % de dégâts et de PV du héros")]
    [InlineData(BuildingKind.Market, 8, "+16 % d'or des monstres")]
    [InlineData(BuildingKind.Workshop, 25, "-12,5 % sur le prix des combattants")]
    [InlineData(BuildingKind.Workshop, 400, "-50 % sur le prix des combattants")]
    [InlineData(BuildingKind.Mill, 30, "absence comptée jusqu'à 13 h")]
    [InlineData(BuildingKind.Mill, 1, "absence comptée jusqu'à 8 h 10")]
    [InlineData(BuildingKind.Farm, 40, "poule dorée 1,4× plus fréquente")]
    [InlineData(BuildingKind.Chapel, 25, "compétences rechargées 1,25× plus vite")]
    [InlineData(BuildingKind.Belfry, 12, "+12 s au chrono des boss")]
    [InlineData(BuildingKind.Belfry, 80, "+30 s au chrono des boss")]
    public void TheCurrentEffectFollowsTheLevel(BuildingKind kind, int level, string expected)
    {
        FrontierState state = new();
        Village.Build(state, kind, level);
        Assert.Equal(expected, EffectText.Now(state, kind));
    }

    [Fact]
    public void TheBakeryShowsTheDeliveredBread()
    {
        FrontierState state = new() { Supply = 3 };
        Village.Build(state, BuildingKind.Bakery, 1);
        Assert.Equal("+15 % de dégâts (3/5 livraisons)", EffectText.Now(state, BuildingKind.Bakery));
    }
}
