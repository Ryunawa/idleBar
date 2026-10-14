using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class SkirmishTests
{
    private const int Deep = 150;

    [Fact]
    public void TheTankDrawsEveryBlow()
    {
        FrontierState state = Team(HeroRole.Tank);
        Assert.All(Targets(state), target => Assert.Equal(FighterKind.Hero, target));
    }

    [Fact]
    public void MonstersAimAtTheMeleeHeroMoreOften()
    {
        Dictionary<FighterKind, int> hits = Targets(Team(HeroRole.Warrior)).GroupBy(target => target).ToDictionary(group => group.Key, group => group.Count());
        Assert.InRange(hits[FighterKind.Hero], 380, 420);
        Assert.InRange(hits[FighterKind.First], 80, 120);
        Assert.InRange(hits[FighterKind.Second], 80, 120);
    }

    [Fact]
    public void ARangedHeroIsAimedAtLikeTheCompanions()
    {
        Dictionary<FighterKind, int> hits = Targets(Team(HeroRole.Archer)).GroupBy(target => target).ToDictionary(group => group.Key, group => group.Count());
        Assert.All(hits.Values, count => Assert.InRange(count, 180, 220));
    }

    [Fact]
    public void AFallenCompanionComesBackAfterThirtySeconds()
    {
        FrontierState state = Team(HeroRole.Warrior);
        EventLog events = new();
        state.Downed[(int)FighterKind.First] = Vitality.ReviveSeconds(state, FighterKind.First);
        state.Health[(int)FighterKind.First] = 0;
        Assert.Equal(30, state.Downed[(int)FighterKind.First]);

        Skirmish.Recover(state, 29.9, events);
        Assert.False(Vitality.Up(state, FighterKind.First));
        Skirmish.Recover(state, 0.2, events);
        Assert.True(Vitality.Up(state, FighterKind.First));
        Assert.Equal(1, state.Health[(int)FighterKind.First]);
        Assert.Contains(events.Drain(), frontierEvent => frontierEvent is Revived { Kind: FighterKind.First });
    }

    [Fact]
    public void EveryoneDownSendsTheFrontBackOneStage()
    {
        FrontierState state = new() { Stage = 15, MaxStage = 15 };
        state.Health[(int)FighterKind.Hero] = 0.0001;
        FrontierGame game = new(state, new Random(1));
        BattleTests.Run(game, 20);
        IReadOnlyList<FrontierEvent> events = game.Events.Drain();
        Assert.Contains(events, frontierEvent => frontierEvent is Routed { Stage: 14, Boss: false });
        Assert.True(Vitality.Up(state, FighterKind.Hero));
        Assert.False(state.BossHeld);
    }

    [Fact]
    public void TheFrontNeverFallsBackPastABeatenBoss()
    {
        FrontierState state = new() { Stage = 15, MaxStage = 15 };
        FrontierGame game = new(state, new Random(1));
        int routs = 0;
        for (int second = 0; second < 300; second++)
        {
            state.Health[(int)FighterKind.Hero] = Math.Min(state.Health[(int)FighterKind.Hero], 0.001);
            game.Advance(1);
            routs += game.Events.Drain().OfType<Routed>().Count();
        }

        Assert.True(routs > 4, $"{routs} reculs");
        Assert.Equal(11, state.Stage);
    }

    [Fact]
    public void FallingToABossHoldsIt()
    {
        FrontierState state = new() { Role = HeroRole.Mage, Stage = 20, MaxStage = 20 };
        FrontierGame game = new(state, new Random(1));
        BattleTests.Run(game, 5);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is Routed { Stage: 19, Boss: true });
        Assert.True(state.BossHeld);
    }

    [Fact]
    public void AWarriorDelaysTheMonsterBlows()
    {
        Assert.True(Blows(HeroRole.Warrior) < Blows(HeroRole.Support) * 0.85);
    }

    [Fact]
    public void BossesTakeMoreDamageWithAMageOnTheFront()
    {
        double regular = HeroBlow(HeroRole.Mage, 61);
        double boss = HeroBlow(HeroRole.Mage, 60);
        Assert.Equal(1 + Balance.MageBossBonus, boss / regular, 6);
    }

    [Fact]
    public void MonstersHitLessAsTheFrontGoesDeeper()
    {
        Assert.Equal(1 + Balance.FrontToughness * Balance.ToughStages, Vitality.Toughness(Balance.ToughStages), 6);
        Assert.Equal(1 + Balance.FrontToughness * Balance.ToughStages + Balance.LateToughness * 30, Vitality.Toughness(Balance.ToughStages + 30), 6);
        Assert.True(Vitality.Toughness(10) < 1 + Balance.FrontToughness * 10);
        Assert.Equal(1, Vitality.MonsterHit(80, false) * Vitality.Toughness(80) / (Economy.MonsterHp(80) * Balance.MonsterHit), 6);
    }

    [Fact]
    public void ArmorRaisesEveryonesHealth()
    {
        FrontierState state = Team(HeroRole.Warrior);
        double before = Vitality.MaxHp(state, FighterKind.Second);
        state.Armor = 10;
        Assert.Equal(2, Vitality.MaxHp(state, FighterKind.Second) / before, 6);
    }

    [Fact]
    public void ReturningRestoresTheTeam()
    {
        FrontierGame game = new(Team(HeroRole.Warrior), new Random(1));
        game.State.Downed[(int)FighterKind.Second] = 20;
        game.State.Health[(int)FighterKind.Hero] = 0.2;
        game.Absent(120);
        Assert.True(Vitality.Up(game.State, FighterKind.Second));
        Assert.Equal(1, game.State.Health[(int)FighterKind.Hero]);
    }

    private static FrontierState Team(HeroRole role) => new() { Role = role, Fighters = [10, 10, 10] };

    private static FrontierState Sturdy(HeroRole role, int stage) =>
        new() { Role = role, Fighters = [139, 139, 139], Stage = stage + Deep, MaxStage = stage + Deep, Armor = int.MaxValue / 2 };

    private static List<FighterKind> Targets(FrontierState state)
    {
        Monster monster = new(3, 0, false, 5, 100, 0);
        List<FighterKind> targets = [];
        for (int strike = 0; strike < 600; strike++)
        {
            monster.Strikes++;
            targets.Add(Skirmish.Target(state, monster)?.Kind ?? throw new InvalidOperationException());
        }

        return targets;
    }

    private static int Blows(HeroRole role)
    {
        FrontierGame game = new(Sturdy(role, 61), new Random(1));
        BattleTests.Run(game, 20);
        return game.Events.Drain().OfType<Struck>().Count();
    }

    private static double HeroBlow(HeroRole role, int stage)
    {
        FrontierGame game = new(Sturdy(role, stage), new Random(1));
        BattleTests.Run(game, 6);
        return game.Events.Drain().OfType<Hit>().First(hit => hit.Source == HitSource.Hero).Amount;
    }
}
