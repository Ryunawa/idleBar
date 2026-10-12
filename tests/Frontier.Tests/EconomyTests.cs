using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class EconomyTests
{
    [Fact]
    public void MonsterHpGrowsFastFirstThenSlower()
    {
        Assert.Equal(Balance.MonsterHpBase, Economy.MonsterHp(1), 6);
        Assert.Equal(Balance.HpCurve[0].Growth, Economy.MonsterHp(2) / Economy.MonsterHp(1), 6);
        Assert.Equal(Balance.HpCurve[1].Growth, Economy.MonsterHp(52) / Economy.MonsterHp(51), 6);
        Assert.Equal(Balance.HpCurve[2].Growth, Economy.MonsterHp(152) / Economy.MonsterHp(151), 6);
    }

    [Fact]
    public void EveryTenthStageIsABoss()
    {
        Assert.True(Economy.IsBossStage(10));
        Assert.True(Economy.IsBossStage(50));
        Assert.False(Economy.IsBossStage(11));
    }

    [Fact]
    public void EachLevelMakesTheNextOneDearer()
    {
        FrontierState state = new();
        double first = Economy.FighterCost(state, FighterKind.Hero);
        state.Fighters[(int)FighterKind.Hero]++;
        Assert.Equal(first * Balance.FighterCostGrowth, Economy.FighterCost(state, FighterKind.Hero), 6);
    }

    [Fact]
    public void TheMillBoostsTheFarm()
    {
        FrontierState state = new();
        state.Buildings[(int)BuildingKind.Farm] = 10;
        double alone = Economy.BuildingGold(state, BuildingKind.Farm);
        state.Buildings[(int)BuildingKind.Mill] = 1;
        Assert.Equal(alone * (1 + Balance.MillBonus), Economy.BuildingGold(state, BuildingKind.Farm), 6);
    }

    [Fact]
    public void SoulsMultiplyDamageAndGold()
    {
        FrontierState state = new();
        state.Buildings[(int)BuildingKind.Farm] = 1;
        double dps = Economy.Dps(state, FighterKind.Hero);
        double gold = Economy.BaseGoldPerSecond(state);
        state.Souls = 40;
        Assert.Equal(dps * 2, Economy.Dps(state, FighterKind.Hero), 6);
        Assert.Equal(gold * 2, Economy.BaseGoldPerSecond(state), 6);
    }

    [Fact]
    public void TheTenthLevelAddsAMilestone()
    {
        FrontierState state = new();
        double ninth = Economy.BaseDps(state, FighterKind.Hero, 9);
        double tenth = Economy.BaseDps(state, FighterKind.Hero, 10);
        Assert.Equal(ninth / 9 * 10 * Balance.MilestoneFactor, tenth, 6);
    }

    [Fact]
    public void FuryDoublesDamage()
    {
        FrontierState state = new();
        double calm = Economy.Dps(state, FighterKind.Hero);
        state.FuryLeft = 1;
        Assert.Equal(calm * Balance.FuryFactor, Economy.Dps(state, FighterKind.Hero), 6);
    }

    [Fact]
    public void TiersCountTheThresholdsReached()
    {
        Assert.Equal(0, Economy.Tier(9));
        Assert.Equal(1, Economy.Tier(10));
        Assert.Equal(3, Economy.Tier(99));
        Assert.Equal(5, Economy.Tier(250));
    }
}
