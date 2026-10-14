using System.Linq;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class TalentTests
{
    [Fact]
    public void EachClassGivesItsPassiveToTheTeam()
    {
        Assert.Equal(1 + Balance.RangerSpeed, Talents.Speed(Hero(HeroRole.Archer)), 6);
        Assert.Equal(Balance.MageBossBonus, Talents.BossDamage(Hero(HeroRole.Mage)), 6);
        Assert.Equal(Balance.WarriorIntimidation, Talents.Intimidation(Hero(HeroRole.Warrior)), 6);
        Assert.Equal(Balance.SupportBlessing, Talents.Blessing(Hero(HeroRole.Support)), 6);
        Assert.Equal(0, Talents.Blessing(Hero(HeroRole.Warrior)), 6);
    }

    [Fact]
    public void ACompanionGivesHalfItsPassive()
    {
        FrontierState state = Hero(HeroRole.Warrior);
        state.Fighters[(int)FighterKind.First] = 10;
        state.Companions[0] = HeroRole.Support;
        Assert.Equal(Balance.SupportBlessing * Balance.CompanionShare, Talents.Blessing(state), 6);
        state.Downed[(int)FighterKind.First] = 10;
        Assert.Equal(0, Talents.Blessing(state), 6);
    }

    [Fact]
    public void ARangerMakesEveryoneAttackFaster()
    {
        FrontierState plain = Hero(HeroRole.Warrior);
        FrontierState ranger = Hero(HeroRole.Warrior);
        ranger.Fighters[(int)FighterKind.First] = 1;
        ranger.Companions[0] = HeroRole.Archer;
        double speed = 1 + Balance.RangerSpeed * Balance.CompanionShare;
        Assert.Equal(Economy.Interval(plain, FighterKind.Hero) / speed, Economy.Interval(ranger, FighterKind.Hero), 6);
        double synergy = Economy.Synergy(ranger) / Economy.Synergy(plain);
        Assert.Equal(Economy.Dps(plain, FighterKind.Hero) * speed * synergy, Economy.Dps(ranger, FighterKind.Hero), 6);
    }

    [Fact]
    public void AGuardianCompanionDrawsHalfTheBlows()
    {
        FrontierState state = Hero(HeroRole.Warrior);
        state.Fighters[(int)FighterKind.First] = 10;
        state.Companions[0] = HeroRole.Tank;
        Monster monster = new(3, 0, false, 5, 100, 0);
        int drawn = 0;
        for (int strike = 0; strike < 600; strike++)
        {
            monster.Strikes++;
            drawn += Skirmish.Target(state, monster)?.Kind == FighterKind.First ? 1 : 0;
        }

        Assert.InRange(drawn, 300, 450);
    }

    [Fact]
    public void AWarriorMakesMonstersStrikeLessOften()
    {
        FrontierState state = Hero(HeroRole.Warrior);
        EventLog events = new();
        Monster monster = new(1, 0, false, 5, 1e12, 0) { StrikeIn = 0.01 };
        state.Monster = monster;
        Skirmish.Update(state, monster, 0.02, events);
        Assert.Equal(Balance.MonsterStrikeSeconds / (1 - Balance.WarriorIntimidation) - 0.01, monster.StrikeIn, 6);
        Assert.Single(events.Drain().OfType<Struck>());
    }

    private static FrontierState Hero(HeroRole role) => new() { Role = role, Fighters = [10, 0, 0] };
}
