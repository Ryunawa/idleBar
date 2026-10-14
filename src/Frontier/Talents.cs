using System.Linq;

namespace IdleBar.Frontier;

public static class Talents
{
    public static double Speed(FrontierState state) => 1 + Balance.RangerSpeed * Share(state, HeroRole.Archer);

    public static double BossDamage(FrontierState state) => Balance.MageBossBonus * Share(state, HeroRole.Mage);

    public static double Intimidation(FrontierState state) => Balance.WarriorIntimidation * Share(state, HeroRole.Warrior);

    public static double Blessing(FrontierState state) => Balance.SupportBlessing * Share(state, HeroRole.Support);

    public static double Share(FrontierState state, HeroRole role)
    {
        if (state.Role == role && Vitality.Up(state, FighterKind.Hero))
        {
            return 1;
        }

        return Team.Members(state).Any(member => !IsHero(member) && Team.Role(state, member) == role && Team.Up(state, member))
            ? Balance.CompanionShare
            : 0;
    }

    public static Combatant? Guardian(FrontierState state, Monster monster)
    {
        if (state.Role == HeroRole.Tank && Vitality.Up(state, FighterKind.Hero))
        {
            return new Combatant(FighterKind.Hero, null);
        }

        return monster.Strikes % 2 == 0 && Team.Tank(state) is Combatant guardian ? guardian : null;
    }

    public static bool IsHero(Combatant member) => member.Ally is null && member.Kind == FighterKind.Hero;
}
