using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class Team
{
    public static IEnumerable<Combatant> Members(FrontierState state)
    {
        foreach (FighterInfo fighter in Fighters.All)
        {
            if (state.Level(fighter.Kind) > 0)
            {
                yield return new Combatant(fighter.Kind, null);
            }
        }

        foreach (Ally ally in state.Allies)
        {
            yield return new Combatant(FighterKind.Hero, ally);
        }
    }

    public static bool Up(FrontierState state, Combatant member) =>
        member.Ally is Ally ally ? ally.Downed <= 0 : Vitality.Up(state, member.Kind);

    public static HeroRole Role(FrontierState state, Combatant member) => member.Ally?.Role ?? state.ClassOf(member.Kind);

    public static bool Ranged(FrontierState state, Combatant member) =>
        Roles.Of(Role(state, member)).Ranged;

    public static Gear Gear(FrontierState state, Combatant member) => member.Ally is null ? state.Gears[(int)member.Kind] : Frontier.Gear.None;

    public static double Health(FrontierState state, Combatant member) => member.Ally?.Health ?? state.Health[(int)member.Kind];

    public static void SetHealth(FrontierState state, Combatant member, double health)
    {
        if (member.Ally is Ally ally)
        {
            ally.Health = health;
        }
        else
        {
            state.Health[(int)member.Kind] = health;
        }
    }

    public static void Down(FrontierState state, Combatant member, double seconds)
    {
        if (member.Ally is Ally ally)
        {
            ally.Downed = seconds;
        }
        else
        {
            state.Downed[(int)member.Kind] = seconds;
        }
    }

    public static double MaxHp(FrontierState state, Combatant member) =>
        member.Ally is Ally ally ? Vitality.AllyMaxHp(state, ally) : Vitality.MaxHp(state, member.Kind);

    public static double ReviveSeconds(FrontierState state, Combatant member) =>
        Fighters.Of(member.Kind).Revive * (SupportUp(state) ? Balance.SupportRevive : 1);

    public static bool AnyUp(FrontierState state) => Members(state).Any(member => Up(state, member));

    public static bool SupportUp(FrontierState state) => Members(state).Any(member => Role(state, member) == HeroRole.Support && Up(state, member));

    public static Combatant? Tank(FrontierState state)
    {
        foreach (Combatant member in Members(state))
        {
            if (Role(state, member) == HeroRole.Tank && Up(state, member))
            {
                return member;
            }
        }

        return null;
    }
}
