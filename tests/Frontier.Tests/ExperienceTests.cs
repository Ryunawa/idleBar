using System;
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
    public void TheClassActionPaysFarMoreThanAPlainBlow(HeroRole role)
    {
        FrontierState state = Steady(role);
        FrontierGame game = new(state, new Random(1));
        double plain = 0;
        double signature = 0;
        for (int step = 0; step < 400; step++)
        {
            double before = state.Xp[0];
            game.Advance(Balance.Step);
            Hit? hit = game.Events.Drain().OfType<Hit>().FirstOrDefault(hit => hit.Source == HitSource.Hero);
            if (hit is not null)
            {
                bool action = role == HeroRole.Archer ? hit.Critical : hit.Big;
                double gained = state.Xp[0] - before;
                plain = action ? plain : Math.Max(plain, gained);
                signature = action ? Math.Max(signature, gained) : signature;
            }
        }

        Assert.True(plain > 0);
        Assert.Equal(Experience.Signature(role) / Balance.XpSmall, signature / plain, 6);
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
    public void ASupportLearnsMostlyByRaisingTheFallen()
    {
        FrontierState state = Steady(HeroRole.Support);
        state.Fighters[(int)FighterKind.First] = 400;
        state.Downed[(int)FighterKind.First] = 0.01;
        double before = state.Xp[0];
        Skirmish.Recover(state, 0.02, new EventLog());
        Assert.Equal(Experience.Unit(state, state.Stage) * Balance.XpRevive, state.Xp[0] - before, 3);
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

    private static FrontierState Steady(HeroRole role) =>
        new() { Role = role, Fighters = [139, 0, 0], Stage = 211, MaxStage = 211, Armor = int.MaxValue / 2 };
}
