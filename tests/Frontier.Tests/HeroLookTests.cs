using System;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class HeroLookTests
{
    [Fact]
    public void TheRoleChangesOnlyAfterARetreat()
    {
        FrontierGame game = new(new FrontierState(), new Random(1));
        HeroLook look = new("Aldric", 1, 2, 3, 4);
        Assert.True(game.State.CanChangeRole);
        Assert.True(game.Embody(look, HeroRole.Tank));
        Assert.False(game.Embody(look, HeroRole.Mage));
        Assert.True(game.Embody(look with { Outfit = 7 }, HeroRole.Tank));
        game.State.Retreats++;
        Assert.True(game.Embody(look, HeroRole.Mage));
        Assert.False(game.Embody(look, HeroRole.Archer));
        Assert.Equal(HeroRole.Mage, game.State.Role);
    }

    [Fact]
    public void AHeroNeedsAValidNameAndLook()
    {
        Assert.False(new HeroLook("Al", 0, 0, 0, 0).IsValid());
        Assert.False(new HeroLook("Aldric<script>", 0, 0, 0, 0).IsValid());
        Assert.False(new HeroLook("Aldric", HeroLook.Skins, 0, 0, 0).IsValid());
        Assert.True(new HeroLook("Jean-Éric d'Arc", 3, 2, 5, 7).IsValid());
        Assert.False(new HeroLook("Алдрик", 0, 0, 0, 0).IsValid());
        Assert.Equal("Jean d'Arc", HeroLook.Tidy("  Jean   d'Arc "));
    }
}
