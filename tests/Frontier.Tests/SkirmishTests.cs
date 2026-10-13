using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class SkirmishTests
{
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
        Assert.InRange(hits[FighterKind.Archer], 80, 120);
        Assert.InRange(hits[FighterKind.Mage], 80, 120);
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
        state.Downed[(int)FighterKind.Archer] = Vitality.ReviveSeconds(state, FighterKind.Archer);
        state.Health[(int)FighterKind.Archer] = 0;
        Assert.Equal(30, state.Downed[(int)FighterKind.Archer]);

        Skirmish.Recover(state, 29.9, events);
        Assert.False(Vitality.Up(state, FighterKind.Archer));
        Skirmish.Recover(state, 0.2, events);
        Assert.True(Vitality.Up(state, FighterKind.Archer));
        Assert.Equal(1, state.Health[(int)FighterKind.Archer]);
        Assert.Contains(events.Drain(), frontierEvent => frontierEvent is Revived { Kind: FighterKind.Archer });
    }

    [Fact]
    public void ASupportBringsTheFallenBackTwiceAsFast()
    {
        FrontierState state = Team(HeroRole.Support);
        Assert.Equal(15, Vitality.ReviveSeconds(state, FighterKind.Archer));
        state.Downed[(int)FighterKind.Hero] = 10;
        Assert.Equal(30, Vitality.ReviveSeconds(state, FighterKind.Archer));
    }

    [Fact]
    public void EveryoneDownSendsTheFrontBackOneStage()
    {
        FrontierState state = new() { Stage = 15, MaxStage = 15 };
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
        FrontierState state = new() { Stage = 12, MaxStage = 12 };
        FrontierGame game = new(state, new Random(1));
        BattleTests.Run(game, 300);
        Assert.True(game.Events.Drain().OfType<Routed>().Count() > 2);
        Assert.Equal(11, state.Stage);
    }

    [Fact]
    public void FallingToABossHoldsIt()
    {
        FrontierState state = new() { Stage = 20, MaxStage = 20 };
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
    public void AnArcherHeroLandsACriticalEveryFifthShot()
    {
        FrontierState state = Sturdy(HeroRole.Archer, 60);
        FrontierGame game = new(state, new Random(1));
        BattleTests.Run(game, 10);
        List<Hit> shots = [.. game.Events.Drain().OfType<Hit>().Where(hit => hit.Source == HitSource.Hero)];
        Assert.True(shots.Count >= 10);
        Assert.Equal(shots.Count / Balance.CriticalEvery, shots.Count(hit => hit.Critical));
        Assert.Equal(Balance.CriticalFactor, shots.First(hit => hit.Critical).Amount / shots.First().Amount, 6);
    }

    [Fact]
    public void AMageHeroHitsBossesHarder()
    {
        double regular = HeroBlow(HeroRole.Mage, 61);
        double boss = HeroBlow(HeroRole.Mage, 60);
        Assert.Equal(Balance.MageBossFactor, boss / regular, 6);
    }

    [Fact]
    public void TheSupportHealsTheTeam()
    {
        FrontierState state = Sturdy(HeroRole.Support, 61);
        Array.Fill(state.Health, 0.5);
        FrontierGame game = new(state, new Random(1));
        BattleTests.Run(game, 5);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is Healed);
        Assert.All(state.Health, health => Assert.True(health >= 0.5 + Balance.HealShare - 0.01));
    }

    [Fact]
    public void ArmorRaisesEveryonesHealth()
    {
        FrontierState state = Team(HeroRole.Warrior);
        double before = Vitality.MaxHp(state, FighterKind.Mage);
        state.Armor = 10;
        Assert.Equal(2, Vitality.MaxHp(state, FighterKind.Mage) / before, 6);
        Assert.True(new FrontierGame(new FrontierState { Gold = 1_000 }, new Random(1)).Buy(new ArmorPurchase()));
    }

    [Fact]
    public void TheRoleChangesOnlyAfterARetreat()
    {
        FrontierGame game = new(new FrontierState(), new Random(1));
        HeroLook look = new("Aldric", 1, 2, 3, 4);
        Assert.True(game.State.CanChangeRole);
        Assert.True(game.Embody(look, HeroRole.Tank));
        Assert.False(game.Embody(look, HeroRole.Mage));
        Assert.True(game.Embody(look with { Outfit = 7 }, HeroRole.Tank));
        game.State.Retreats++;
        Assert.True(game.Embody(look, HeroRole.Mage));
        Assert.False(game.Embody(look, HeroRole.Archer));
        Assert.Equal(HeroRole.Mage, game.State.Role);
    }

    [Fact]
    public void AHeroNeedsAValidNameAndLook()
    {
        Assert.False(new HeroLook("Al", 0, 0, 0, 0).IsValid());
        Assert.False(new HeroLook("Aldric<script>", 0, 0, 0, 0).IsValid());
        Assert.False(new HeroLook("Aldric", HeroLook.Skins, 0, 0, 0).IsValid());
        Assert.True(new HeroLook("Jean-Éric d'Arc", 3, 2, 5, 7).IsValid());
    }

    [Fact]
    public void ReturningRestoresTheTeam()
    {
        FrontierGame game = new(Team(HeroRole.Warrior), new Random(1));
        game.State.Downed[(int)FighterKind.Mage] = 20;
        game.State.Health[(int)FighterKind.Hero] = 0.2;
        game.Absent(120);
        Assert.True(Vitality.Up(game.State, FighterKind.Mage));
        Assert.Equal(1, game.State.Health[(int)FighterKind.Hero]);
    }

    private static FrontierState Team(HeroRole role) => new() { Role = role, Fighters = [10, 10, 10] };

    private static FrontierState Sturdy(HeroRole role, int stage) =>
        new() { Role = role, Fighters = [1, 1, 1], Stage = stage, MaxStage = stage, Armor = int.MaxValue / 2 };

    private static List<FighterKind> Targets(FrontierState state)
    {
        Monster monster = new(3, 0, false, 5, 100, 0);
        List<FighterKind> targets = [];
        for (int strike = 0; strike < 600; strike++)
        {
            monster.Strikes++;
            targets.Add(Skirmish.Target(state, monster) ?? throw new InvalidOperationException());
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
