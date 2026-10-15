using System.Linq;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class GearStatTests
{
    [Fact]
    public void GoldGearRaisesTheGoldOfEveryMonsterForTheWholeTeam()
    {
        FrontierState state = new() { Fighters = [10, 10, 0], Stage = 20, MaxStage = 20 };
        double plain = Economy.KillGold(state, true);
        state.Gears[(int)FighterKind.Hero] = Gear.None with { Gold = 0.1 };
        state.Gears[(int)FighterKind.First] = Gear.None with { Gold = 0.2 };
        state.Gears[(int)FighterKind.Second] = Gear.None with { Gold = 2 };
        Assert.Equal(1.3, Economy.KillGold(state, true) / plain, 6);
        Assert.Equal(1.3, Economy.Plunder(state), 6);
        state.Fighters[(int)FighterKind.Second] = 10;
        Assert.Equal(1 + Balance.PlunderCap, Economy.Plunder(state), 6);
    }

    [Fact]
    public void SpeedGearStrikesMoreOftenWithoutSmallerBlows()
    {
        FrontierState state = new() { Fighters = [10, 0, 0], Stage = 20, MaxStage = 20 };
        double dps = Economy.Dps(state, FighterKind.Hero);
        double blow = Economy.Dps(state, FighterKind.Hero) * Economy.Interval(state, FighterKind.Hero);
        state.Gears[(int)FighterKind.Hero] = Gear.None with { Speed = 0.25 };
        Assert.Equal(1.25, Economy.Dps(state, FighterKind.Hero) / dps, 6);
        Assert.Equal(blow, Economy.Dps(state, FighterKind.Hero) * Economy.Interval(state, FighterKind.Hero), 6);
    }

    [Fact]
    public void AFractionOfAPercentOfCriticalStillCrits()
    {
        FrontierState state = new() { Fighters = [10, 0, 0], Stage = 20, MaxStage = 20 };
        state.Gears[(int)FighterKind.Hero] = Gear.None with { Critical = 0.004 };
        Monster monster = new(1, 0, false, 20, 1e15, 0);
        state.Monster = monster;
        EventLog events = new();
        for (int blow = 0; blow < 500; blow++)
        {
            Blows.Swing(state, FighterKind.Hero, monster, 1, events);
        }

        Assert.Equal(2, events.Drain().OfType<Hit>().Count(hit => hit.Critical));
    }

    [Fact]
    public void XpGearSpeedsUpOnlyItsWearer()
    {
        FrontierState state = new() { Fighters = [10, 10, 0] };
        state.Gears[(int)FighterKind.Hero] = Gear.None with { Xp = 3 };
        Experience.Gain(state, FighterKind.Hero, 0.01, 30, new EventLog());
        Experience.Gain(state, FighterKind.First, 0.01, 30, new EventLog());
        Assert.Equal(1 + Balance.GearXpCap, state.Xp[(int)FighterKind.Hero] / state.Xp[(int)FighterKind.First], 6);
    }

    [Fact]
    public void RechargeGearShortensTheActiveOfItsWearer()
    {
        FrontierState state = new() { Fighters = [10, 10, 0] };
        double hero = Actives.Cooldown(state, FighterKind.Hero);
        double first = Actives.Cooldown(state, FighterKind.First);
        state.Gears[(int)FighterKind.Hero] = Gear.None with { Recharge = 0.25 };
        Assert.Equal(1.25, hero / Actives.Cooldown(state, FighterKind.Hero), 6);
        Assert.Equal(first, Actives.Cooldown(state, FighterKind.First));
    }

    [Fact]
    public void BossGearHitsOnlyBossesHarder()
    {
        FrontierState state = new() { Fighters = [10, 10, 0], Stage = 20, MaxStage = 20 };
        state.Gears[(int)FighterKind.Hero] = Gear.None with { Boss = 0.5 };
        Assert.Equal(150, Dealt(state, true, HitSource.Hero), 6);
        Assert.Equal(150, Dealt(state, true, HitSource.Click), 6);
        Assert.Equal(100, Dealt(state, true, HitSource.First), 6);
        Assert.Equal(100, Dealt(state, false, HitSource.Hero), 6);
    }

    [Fact]
    public void EachSlotRollsItsOwnStats()
    {
        Assert.Equal([Items.Health, Items.Healing, Items.Xp], ItemStats.Pool(Items.Legs, null));
        Assert.Equal([Items.Health, Items.Speed, Items.Recharge], ItemStats.Pool(Items.Feet, null));
        Assert.Equal([Items.Damage, Items.Critical, Items.Speed], ItemStats.Pool(Items.Arms, null));
        Assert.Equal(Items.Stats, ItemStats.Pool(Items.Ring, null));
        Assert.Equal(Items.Stats, ItemStats.Pool(Items.Helm, null));
        Assert.Equal([Items.Health, Items.Healing, Items.Gold, Items.Xp], ItemStats.Pool(Items.Armor, null));
        Assert.Equal([Items.Damage, Items.Speed, Items.Gold, Items.Boss], ItemStats.Pool(Items.Weapon, HeroRole.Warrior));
        Assert.Equal([Items.Damage, Items.Health, Items.Healing, Items.Xp], ItemStats.Pool(Items.Weapon, HeroRole.Support));
        Assert.Equal("PV, vitesse ou recharge de l'actif", ItemStats.Line(Items.Feet));
        Assert.Equal("anneau, bague, chevalière, alliance, jonc, solitaire, torsade ou cachet", Items.LooksLine(Items.Ring));
    }

    [Fact]
    public void TheSummaryShowsTheCaps()
    {
        Gear gear = Gear.None with { Damage = 0.125, Xp = 0.8, Recharge = 0.1 };
        Assert.Equal("Bonus portés : +12,5 % de dégâts · +60 % d'or des monstres pour l'équipe (plafond : 50 %) · +80 % d'xp (plafond : 50 %) · +10 % de recharge de l'actif",
            GearSummary.Describe(gear, 0.6));
        Assert.Equal("Aucun bonus porté pour l'instant.", GearSummary.Describe(Gear.None, 0));
    }

    private static double Dealt(FrontierState state, bool boss, HitSource source)
    {
        Monster monster = new(1, 0, boss, state.Stage, 1e9, 0);
        state.Monster = monster;
        Battle.Damage(state, 100, source, new EventLog());
        return 1e9 - monster.Hp;
    }
}
