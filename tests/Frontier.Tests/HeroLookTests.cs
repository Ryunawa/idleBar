using System;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class HeroLookTests
{
    [Fact]
    public void TheServerHeroIsAdopted()
    {
        FrontierGame game = new(new FrontierState(), new Random(1));
        HeroLook look = new("Aldric", 1, 2, 3, 4);
        Assert.True(game.State.CanChangeRole);
        game.Adopt(look, HeroRole.Tank, 0);
        Assert.False(game.State.CanChangeRole);
        game.State.Retreats++;
        Assert.True(game.State.CanChangeRole);
        game.Adopt(look with { Outfit = 7 }, HeroRole.Mage, 5);
        Assert.Equal(HeroRole.Mage, game.State.Role);
        Assert.Equal(7, game.State.Hero?.Outfit);
        Assert.False(game.State.CanChangeRole);
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
