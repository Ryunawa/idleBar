using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class ExperienceTests
{
    [Theory]
    [InlineData(HeroRole.Warrior)]
    [InlineData(HeroRole.Archer)]
    [InlineData(HeroRole.Mage)]
    public void AnActivePaysFarMoreThanAPlainBlow(HeroRole role)
    {
        FrontierState state = Steady(role);
        FrontierGame game = new(state, new Random(1));
        double plain = 0;
        double active = 0;
        for (int step = 0; step < 400; step++)
        {
            double before = state.Xp[0];
            game.Advance(Balance.Step);
            IReadOnlyList<FrontierEvent> events = game.Events.Drain();
            double gained = state.Xp[0] - before;
            if (events.Any(frontierEvent => frontierEvent is ActiveUsed { Kind: FighterKind.Hero }))
            {
                active = Math.Max(active, gained);
            }
            else if (events.Any(frontierEvent => frontierEvent is Hit { Source: HitSource.Hero }))
            {
                plain = Math.Max(plain, gained);
            }
        }

        Assert.True(plain > 0);
        Assert.True(active > 20 * plain, $"{active} contre {plain}");
    }

    [Fact]
    public void ATankLearnsByTakingBlows()
    {
        FrontierState state = Steady(HeroRole.Tank);
        FrontierGame game = new(state, new Random(1));
        BattleTests.Run(game, 10);
        int struck = game.Events.Drain().OfType<Struck>().Count(blow => blow.Target == FighterKind.Hero);
        double unit = Experience.Unit(state, state.Stage);
        Assert.True(struck > 3);
        Assert.InRange(state.Xp[0], struck * unit * Balance.XpTaken, (struck + 1) * unit * Balance.XpTaken + 200 * unit * Balance.XpSmall);
    }

    [Fact]
    public void AClericLearnsByRaisingTheFallen()
    {
        FrontierState state = Steady(HeroRole.Support);
        state.Fighters[(int)FighterKind.First] = 139;
        state.Downed[(int)FighterKind.First] = 20;
        Monster monster = new(1, 0, false, state.Stage, 1e300, 0);
        state.Monster = monster;
        double before = state.Xp[0];
        Actives.Update(state, monster, 0.01, new EventLog());
        Assert.Equal(Experience.Unit(state, state.Stage) * (Balance.XpActive * Balance.RaiseCooldown + Balance.XpRevive), state.Xp[0] - before, 3);
    }

    [Fact]
    public void AFallCostsAShareOfTheNextLevelButNeverTheLevel()
    {
        FrontierState state = new() { Role = HeroRole.Warrior, Fighters = [10, 0, 0], Stage = 5, MaxStage = 5 };
        double level = Experience.ToNext(state, FighterKind.Hero);
        state.Xp[0] = level * 0.5;
        Assert.Equal(Balance.FallXp, Fall(state).Lost, 6);
        Assert.Equal(level * (0.5 - Balance.FallXp), state.Xp[0], 6);
        state.Xp[0] = level * Balance.FallXp / 2;
        Assert.Equal(Balance.FallXp / 2, Fall(state).Lost, 6);
        Assert.Equal(0, state.Xp[0]);
        Assert.Equal(0, Fall(state).Lost);
        Assert.Equal(10, state.Level(FighterKind.Hero));
    }

    [Fact]
    public void AFallenReinforcementCostsTheHeroNothing()
    {
        FrontierState state = new() { Role = HeroRole.Warrior, Fighters = [10, 0, 0], Stage = 5, MaxStage = 5 };
        state.Xp[0] = Experience.ToNext(state, FighterKind.Hero) * 0.5;
        state.Downed[0] = 20;
        Ally ally = new(7, new HeroLook("Brune", 1, 1, 1, 1), HeroRole.Warrior, 1, 1800) { Health = 1e-9 };
        state.Allies.Add(ally);
        Monster monster = new(1, 0, false, state.Stage, 1e12, 0) { StrikeIn = 0.01 };
        state.Monster = monster;
        EventLog events = new();
        Skirmish.Update(state, monster, 0.02, events);
        Fell fell = events.Drain().OfType<Fell>().Single();
        Assert.Equal(ally.Id, fell.Ally);
        Assert.Equal(0, fell.Lost);
        Assert.Equal(Experience.ToNext(state, FighterKind.Hero) * 0.5, state.Xp[0], 6);
    }

    [Fact]
    public void EnoughExperienceRaisesTheLevel()
    {
        FrontierState state = new();
        EventLog events = new();
        double needed = Experience.ToNext(state, FighterKind.Hero);
        Experience.Gain(state, FighterKind.Hero, needed / Experience.Unit(state, 1) + 0.01, 1, events);
        Assert.Equal(2, state.Level(FighterKind.Hero));
        Assert.Contains(events.Drain(), frontierEvent => frontierEvent is LeveledUp { Kind: FighterKind.Hero, Level: 2 });
        Experience.Gain(state, FighterKind.Second, 1e9, 1, events);
        Assert.Equal(0, state.Level(FighterKind.Second));
    }

    [Fact]
    public void ARetreatClearsTheExperienceAndTheSaveKeepsIt()
    {
        FrontierState state = new() { Stage = 62, MaxStage = 62, BestStage = 62, LifetimeGold = 5e6, Fighters = [40, 20, 10] };
        state.Xp[0] = 123.5;
        FrontierSave? save = SaveCodec.Deserialize(SaveCodec.Serialize(SaveCodec.Capture(state)));
        Assert.Equal(123.5, SaveCodec.Restore(save!).Xp[0]);
        new FrontierGame(state, new Random(1)).Retreat();
        Assert.All(state.Xp, xp => Assert.Equal(0, xp));
        Assert.Equal(new[] { 1, 0, 0 }, state.Fighters);
    }

    [Fact]
    public void GearPricesFollowTheServerFormula()
    {
        FrontierState state = new() { Stage = 31, MaxStage = 31, BestStage = 31 };
        double common = 30 * (Economy.MonsterHp(31) * 0.35 + 2);
        Assert.Equal(common, GearPrices.Offer(state, 31, 0), 3);
        Assert.Equal(common * 15, GearPrices.Offer(state, 31, 2), 3);
        Assert.Equal(common * 0.5 * (Math.Pow(1.25, 3) - 1) / 0.25, GearPrices.Upgrade(state, 0, 0, 3), 3);
        Assert.Equal(1.6, Items.Upgraded(1, 3), 6);
    }

    private static Fell Fall(FrontierState state)
    {
        state.Health[0] = 1e-9;
        Monster monster = new(1, 0, false, state.Stage, 1e12, 0) { StrikeIn = 0.01 };
        state.Monster = monster;
        EventLog events = new();
        Skirmish.Update(state, monster, 0.02, events);
        return events.Drain().OfType<Fell>().Single(fell => fell.Kind == FighterKind.Hero && fell.Ally == 0);
    }

    private static FrontierState Steady(HeroRole role) =>
        new() { Role = role, Fighters = [139, 0, 0], Stage = 211, MaxStage = 211, Armor = int.MaxValue / 2 };
}
