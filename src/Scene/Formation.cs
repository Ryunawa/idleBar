using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class Formation
{
    private const int RoomySpacing = 18;
    private const int CompactSpacing = 15;
    private const int RoomyReach = 20;
    private const int CompactReach = 14;

    private static readonly HeroRole[] Order = [HeroRole.Support, HeroRole.Mage, HeroRole.Archer, HeroRole.Warrior, HeroRole.Tank];

    public static IReadOnlyList<Post> Posts(FrontierState state, FrontierLayout layout)
    {
        List<Combatant> members = [.. Team.Members(state).OrderBy(member => Rank(state, member))];
        int last = members.Count - 1;
        int reach = layout.HeroX - layout.FrontX + (layout.Compact ? CompactReach : RoomyReach);
        int spacing = last <= 0 ? 0 : Math.Min(layout.Compact ? CompactSpacing : RoomySpacing, reach / last);
        return [.. members.Select((member, index) => new Post(member, layout.HeroX - spacing * (last - index)))];
    }

    public static int X(FrontierState state, FrontierLayout layout, FighterKind kind) =>
        Posts(state, layout).FirstOrDefault(post => post.Member.Ally is null && post.Member.Kind == kind)?.X ?? layout.FighterX(kind);

    public static int? AllyX(FrontierState state, FrontierLayout layout, long id) =>
        Posts(state, layout).FirstOrDefault(post => post.Member.AllyId == id)?.X;

    private static int Rank(FrontierState state, Combatant member)
    {
        HeroRole role = Team.Role(state, member);
        int standing = member.Ally is Ally ally ? -state.Allies.IndexOf(ally) - 1 : member.Kind == FighterKind.Hero ? 1 : 0;
        return Array.IndexOf(Order, role) * 10 + standing;
    }
}
