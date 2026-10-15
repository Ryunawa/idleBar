using System;
using System.Linq;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class SkillCastTests
{
    [Fact]
    public void TheSmiteHitsHarderWithThunder()
    {
        FrontierState state = Equipped(SkillKind.Smite);
        state.Learned[(int)SkillKind.Thunder] = 2;
        state.Monster = new Monster(1, 1, false, 30, 1e15, 0);
        FrontierGame game = new(state, new Random(1));
        double expected = Economy.HeroPower(state) * Balance.SmiteInterval * (Balance.SmiteHits + 2 * Balance.ThunderHits);
        Assert.True(game.UseSkill(0));
        Hit hit = game.Events.Drain().OfType<Hit>().Single();
        Assert.Equal(HitSource.Smite, hit.Source);
        Assert.Equal(expected, hit.Amount, expected * 1e-9);
        Assert.Equal(Economy.SkillCooldown(state, SkillKind.Smite), SkillCasts.Wait(state, SkillKind.Smite));
        Assert.Equal(1, state.SkillsUsed);
        Assert.False(game.UseSkill(0));
    }

    [Fact]
    public void StrikesWaitForAMonsterAndEmptySlotsDoNothing()
    {
        FrontierState state = Equipped(SkillKind.Judgment);
        FrontierGame game = new(state, new Random(1));
        Assert.False(game.UseSkill(0));
        Assert.Equal(0, SkillCasts.Wait(state, SkillKind.Judgment));
        Assert.False(game.UseSkill(1));
        Assert.False(game.UseSkill(FrontierState.SlotCount));
    }

    [Fact]
    public void TheStormOnlyRaisesBossDamage()
    {
        FrontierState state = Equipped(SkillKind.Storm);
        state.Monster = new Monster(1, 1, true, 30, 1e15, 0);
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.UseSkill(0));
        Assert.Equal(20, state.StormLeft);
        Battle.Damage(state, 100, HitSource.Click, game.Events);
        state.Monster = new Monster(2, 1, false, 30, 1e15, 0);
        Battle.Damage(state, 100, HitSource.Click, game.Events);
        Assert.Equal([150.0, 100.0], game.Events.Drain().OfType<Hit>().Select(hit => hit.Amount));
    }

    [Theory]
    [InlineData(false, 50)]
    [InlineData(true, 160)]
    public void JudgmentTakesAShareOfTheMaximumHealth(bool boss, double expected)
    {
        FrontierState state = Equipped(SkillKind.Judgment);
        state.Role = HeroRole.Mage;
        state.StormLeft = 10;
        state.Monster = new Monster(1, 1, boss, 30, boss ? 8_000 : 1_000, 0) { Hp = 900 };
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.UseSkill(0));
        Assert.Equal(expected, game.Events.Drain().OfType<Hit>().Single(hit => hit.Source == HitSource.Judgment).Amount, 6);
    }

    [Fact]
    public void FuryAndTheHolyShieldLastLongerWithTheirUpgrades()
    {
        FrontierState state = Equipped(SkillKind.Fury, SkillKind.HolyShield);
        state.Learned[(int)SkillKind.LastingRage] = 2;
        state.Learned[(int)SkillKind.Tenacity] = 3;
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.UseSkill(0));
        Assert.True(game.UseSkill(1));
        Assert.Equal(50, state.FuryLeft);
        Assert.Equal(12, state.ShieldLeft);
    }

    [Fact]
    public void TheHolyShieldBlocksEveryBlow()
    {
        FrontierState state = Equipped(SkillKind.HolyShield);
        state.Monster = new Monster(1, 1, true, 40, 1e15, 0);
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.UseSkill(0));
        BattleTests.Run(game, 5);
        Assert.DoesNotContain(game.Events.Drain(), frontierEvent => frontierEvent is Struck);
        Assert.Equal(1, state.Health[(int)FighterKind.Hero]);
        BattleTests.Run(game, 5);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is Struck);
    }

    [Fact]
    public void TheRallyRaisesTheFallenAtHalfHealth()
    {
        FrontierState state = Equipped(SkillKind.Rally);
        state.Fighters = [10, 10, 10];
        FrontierGame game = new(state, new Random(1));
        Assert.False(game.UseSkill(0));
        state.Downed[(int)FighterKind.Hero] = 30;
        state.Downed[(int)FighterKind.Second] = 12;
        state.Health[(int)FighterKind.Hero] = 0;
        state.Health[(int)FighterKind.Second] = 0;
        Assert.True(game.UseSkill(0));
        Assert.All(state.Downed, downed => Assert.Equal(0, downed));
        Assert.Equal(new double[] { 0.5, 1, 0.5 }, state.Health);
        Assert.Equal(2, game.Events.Drain().OfType<Revived>().Count());
    }

    [Fact]
    public void TheGoldRainGrowsWithTheDownpour()
    {
        FrontierState state = Equipped(SkillKind.GoldRain);
        state.Learned[(int)SkillKind.Downpour] = 2;
        Village.Build(state, BuildingKind.Farm, 40);
        FrontierGame game = new(state, new Random(1));
        double expected = Math.Max(Economy.GoldPerSecond(state) * 60, Economy.KillGold(state, false) * Balance.GoldRainKills * 2);
        Assert.True(game.UseSkill(0));
        Assert.Equal(expected, state.Gold, expected * 1e-9);
    }

    [Fact]
    public void TheHarvestTriplesMonsterGold()
    {
        FrontierState state = Equipped(SkillKind.Harvest);
        double before = Economy.KillGold(state, true);
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.UseSkill(0));
        Assert.Equal(30, state.HarvestLeft);
        Assert.Equal(before * 3, Economy.KillGold(state, true), before * 1e-9);
    }

    [Fact]
    public void TheLureBringsTheChickenWithoutDelayingTheNextOne()
    {
        FrontierState state = Equipped(SkillKind.Lure);
        state.ChickenTimer = 100;
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.UseSkill(0));
        Assert.Equal(0, state.Chicken);
        Assert.Contains(game.Events.Drain(), frontierEvent => frontierEvent is ChickenAppeared);
        state.Cooldowns[(int)SkillKind.Lure] = 0;
        Assert.False(game.UseSkill(0));
        BattleTests.Run(game, Balance.ChickenFlight + 1);
        Assert.Null(state.Chicken);
        Assert.InRange(state.ChickenTimer, 100 - Balance.ChickenFlight - 1.1, 100 - Balance.ChickenFlight - 0.9);
    }

    [Fact]
    public void AChickenDueDuringTheLuredOneComesRightAfter()
    {
        FrontierState state = Equipped(SkillKind.Lure);
        state.ChickenTimer = 5;
        FrontierGame game = new(state, new Random(1));
        Assert.True(game.UseSkill(0));
        BattleTests.Run(game, Balance.ChickenFlight + 1);
        Assert.Equal(2, game.Events.Drain().OfType<ChickenAppeared>().Count());
        Assert.NotNull(state.Chicken);
        Assert.False(state.Lured);
        BattleTests.Run(game, Balance.ChickenFlight + 1);
        Assert.Null(state.Chicken);
        Assert.InRange(state.ChickenTimer, Balance.ChickenMin / Economy.ChickenPace(state) - 2, Balance.ChickenMax / Economy.ChickenPace(state));
    }

    [Fact]
    public void RechargeUpgradesOnlyShortenTheirOwnBranch()
    {
        FrontierState state = new();
        state.Learned[(int)SkillKind.LightningRod] = 3;
        state.Learned[(int)SkillKind.Prosperity] = 2;
        Village.Build(state, BuildingKind.Chapel, 50);
        Assert.Equal(600 / 2 / 1.3, Economy.SkillCooldown(state, SkillKind.Smite), 6);
        Assert.Equal(1_800 / 2 / 1.3, Economy.SkillCooldown(state, SkillKind.Judgment), 6);
        Assert.Equal(900 / 2.0, Economy.SkillCooldown(state, SkillKind.Fury), 6);
        Assert.Equal(1_200 / 2 / 1.2, Economy.SkillCooldown(state, SkillKind.Harvest), 6);
        state.Learned[(int)SkillKind.GoldRain] = 1;
        state.Slots[2] = SkillKind.GoldRain;
        Assert.True(new FrontierGame(state, new Random(1)).UseSkill(2));
        Assert.Equal(1_800 / 2 / 1.2, SkillCasts.Wait(state, SkillKind.GoldRain), 6);
    }

    private static FrontierState Equipped(params SkillKind[] kinds)
    {
        FrontierState state = new() { Stage = 30, MaxStage = 30, BestStage = 280 };
        for (int slot = 0; slot < kinds.Length; slot++)
        {
            state.Learned[(int)kinds[slot]] = 1;
            state.Slots[slot] = kinds[slot];
        }

        return state;
    }
}
