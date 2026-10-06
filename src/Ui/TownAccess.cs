using System;
using System.Linq;
using IdleBar.Trade;

namespace IdleBar.Ui;

public sealed record TownAccess(bool Itinerant, bool Crafts, bool Branches, bool Travelling)
{
    public bool Ships => Itinerant || Branches;

    public TownTab DefaultTab => this switch
    {
        { Travelling: true } => TownTab.Journal,
        { Crafts: true } => TownTab.Workshop,
        { Branches: true } => TownTab.Counter,
        _ => TownTab.Market,
    };

    public static TownAccess For(WorldData world, GameSnapshot snapshot, DateTimeOffset now)
    {
        string craftId = snapshot.Player?.CraftId ?? string.Empty;
        return new TownAccess(
            snapshot.Caravan is not null,
            snapshot.Workshop is not null && world.RecipesOf(craftId).Any(),
            world.FindCraft(craftId)?.OpensBranches == true,
            snapshot.Caravan?.IsTravelling(now) == true);
    }

    public bool Shows(TownTab tab) => tab switch
    {
        TownTab.Journal or TownTab.Mastery => true,
        TownTab.Orders => !Itinerant,
        _ when Travelling => false,
        TownTab.Workshop => Crafts,
        TownTab.Contracts => Ships,
        TownTab.Routes or TownTab.Caravan => Itinerant,
        TownTab.Branches => Branches,
        _ => true,
    };
}
