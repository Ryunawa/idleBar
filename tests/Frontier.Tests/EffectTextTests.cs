using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class EffectTextTests
{
    [Theory]
    [InlineData(BuildingKind.Forge, 30, "+90 % de dégâts et de PV du héros")]
    [InlineData(BuildingKind.Market, 8, "+16 % d'or des monstres")]
    [InlineData(BuildingKind.Workshop, 25, "-12,5 % sur l'équipement et l'armure")]
    [InlineData(BuildingKind.Workshop, 400, "-50 % sur l'équipement et l'armure")]
    [InlineData(BuildingKind.Mill, 30, "absence comptée jusqu'à 13 h")]
    [InlineData(BuildingKind.Mill, 1, "absence comptée jusqu'à 8 h 10")]
    [InlineData(BuildingKind.Farm, 40, "poule dorée 1,4× plus fréquente")]
    [InlineData(BuildingKind.Chapel, 25, "compétences rechargées 1,5× plus vite")]
    [InlineData(BuildingKind.Chapel, 400, "compétences rechargées 3× plus vite")]
    [InlineData(BuildingKind.Portal, 30, "+15 % de tout l'or")]
    [InlineData(BuildingKind.Belfry, 12, "+6 s au chrono des boss")]
    [InlineData(BuildingKind.Belfry, 13, "+6,5 s au chrono des boss")]
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
        FrontierState state = new();
        state.Rations.AddRange([25, 20, 10]);
        Village.Build(state, BuildingKind.Bakery, 1);
        Assert.Equal("+15 % de dégâts (3/5 livraisons), convois 1,01× plus fréquents", EffectText.Now(state, BuildingKind.Bakery));
    }
}
