using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class TitanTests
{
    [Fact]
    public void TheTitanIsAnnouncedOncePerWeek()
    {
        FrontierGame game = new(new FrontierState(), new Random(1));
        game.Sight(View(damage: 30_000));
        game.Sight(View(damage: 30_000));
        Assert.Single(game.Events.Drain().OfType<TitanAppeared>());
        game.Sight(View(damage: 0) with { Week = "2026-10-19" });
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is TitanAppeared { Left: 1 });
    }

    [Fact]
    public void EachQuarterOfItsHealthIsAnnounced()
    {
        FrontierGame game = new(new FrontierState(), new Random(1));
        game.Sight(View(damage: 10_000));
        game.Sight(View(damage: 40_000));
        game.Sight(View(damage: 45_000));
        game.Sight(View(damage: 95_000));
        List<TitanWounded> wounds = [.. game.Events.Drain().OfType<TitanWounded>()];
        Assert.Equal(2, wounds.Count);
        Assert.Equal(0.6, wounds[0].Left, 6);
    }

    [Fact]
    public void ItsFallAndThePlayersBlowsAreAnnounced()
    {
        FrontierGame game = new(new FrontierState(), new Random(1));
        game.Sight(View(damage: 90_000, mine: 400));
        game.Sight(View(damage: 100_000, mine: 520) with { Fallen = true });
        IReadOnlyList<FrontierEvent> events = game.Events.Drain();
        Assert.Contains(events, frontierEvent => frontierEvent is TitanStruck { Monsters: 120 });
        Assert.Contains(events, frontierEvent => frontierEvent is TitanFell { Kind: 2 });
        Assert.Equal("Wyrm de givre", Titans.Name(2));
    }

    [Fact]
    public void TheTitanLoomsInTheFogAndCanBeClicked()
    {
        FrontierState state = new() { Titan = View(damage: 50_000) };
        FrontierLayout layout = FrontierLayout.For(815, FrontierLayout.Rows);
        ArtPoint place = Assert.NotNull(TitanPainter.Place(state, layout));
        Assert.Equal(TargetKind.Titan, LaneHits.Find(state, layout, new FrontierScene(new Random(1)), place.X + 16, 20).Kind);
        Assert.Null(TitanPainter.Place(new FrontierState(), layout));
        Assert.Equal(32, TitanPainter.Sprites(3).Body.Width);
    }

    private static TitanView View(long damage, long mine = 0) => new("2026-10-12", 2, 100_000, damage, false, mine);
}
