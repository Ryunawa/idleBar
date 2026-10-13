using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class FormationTests
{
    [Theory]
    [InlineData(815)]
    [InlineData(538)]
    public void TheUsualTeamKeepsItsPlaces(int width)
    {
        FrontierLayout layout = FrontierLayout.For(width, FrontierLayout.Rows);
        FrontierState state = new() { Fighters = [10, 10, 10] };
        Assert.Equal([layout.MageX, layout.ArcherX, layout.HeroX], Formation.Posts(state, layout).Select(post => post.X));
    }

    [Fact]
    public void TheFrontLinesUpByRole()
    {
        FrontierLayout layout = FrontierLayout.For(815, FrontierLayout.Rows);
        FrontierState state = new() { Role = HeroRole.Warrior, Fighters = [10, 10, 10] };
        state.Allies.AddRange([Ally(1, HeroRole.Tank), Ally(2, HeroRole.Warrior), Ally(3, HeroRole.Support)]);
        IReadOnlyList<Post> posts = Formation.Posts(state, layout);
        Assert.Equal(["ally 3", "Mage", "Archer", "ally 2", "Hero", "ally 1"], posts.Select(post => post.Member.Ally is Ally ally ? $"ally {ally.Id}" : post.Member.Kind.ToString()));
        Assert.Equal(layout.HeroX, posts[^1].X);
        Assert.True(posts.Zip(posts.Skip(1)).All(pair => pair.Second.X > pair.First.X));
    }

    [Fact]
    public void ASupportHeroStandsAtTheBack()
    {
        FrontierLayout layout = FrontierLayout.For(815, FrontierLayout.Rows);
        FrontierState state = new() { Role = HeroRole.Support, Fighters = [10, 10, 10] };
        Assert.Equal(layout.MageX, Formation.X(state, layout, FighterKind.Hero));
        Assert.Equal(layout.HeroX, Formation.X(state, layout, FighterKind.Archer));
    }

    [Fact]
    public void LongNamesShrinkToTheirRoom()
    {
        Assert.Equal("Jean-Éric d'Arc", AllyNames.Fit("Jean-Éric d'Arc", int.MaxValue));
        string fitted = AllyNames.Fit("Jean-Éric d'Arc", 14);
        Assert.True(ArtFont.Width(fitted) <= 14, fitted);
        Assert.Equal("Bru", AllyNames.Fit("Brunehilde", 2));
    }

    private static Ally Ally(long id, HeroRole role) => new(id, new HeroLook("Brune", 1, 1, 1, 1), role, 1, 900);
}
