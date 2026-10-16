using System.Linq;
using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class ResearchTests
{
    [Fact]
    public void WithoutAGuildNothingChanges()
    {
        FrontierState state = Front();
        Assert.Null(state.Research);
        Assert.Equal(1, GuildResearch.DamageFactor(state));
        Assert.Equal(1, GuildResearch.HealthFactor(state));
        Assert.Equal(1, GuildResearch.BossFactor(state));
        Assert.Equal(1, GuildResearch.GoldFactor(state));
        Assert.Equal(1, GuildResearch.XpFactor(state));
        Assert.Equal(1, GuildResearch.Discount(state));
        Assert.Equal(1, GuildResearch.RechargePace(state));
        Assert.Equal(30, GuildResearch.AllyMinutes(null));
        Assert.Equal(50, GuildResearch.ConvoyGold(null));
        Assert.Equal(240, GuildResearch.CooldownMinutes(null));
        Assert.Equal(3, GuildResearch.FrontMax(null));
    }

    [Fact]
    public void CommonForgeAddsDamageToTheWholeFront()
    {
        FrontierState plain = Front();
        FrontierState guild = Front((ResearchKind.CommonForge, 2));
        Assert.Equal(Economy.Force(plain, FighterKind.Hero) * 1.08, Economy.Force(guild, FighterKind.Hero), 6);
        Assert.Equal(Economy.TotalDps(plain) * 1.08, Economy.TotalDps(guild), 6);
        Assert.Equal(Economy.ClickDamage(plain) * 1.08, Economy.ClickDamage(guild), 6);
    }

    [Fact]
    public void RampartsAddHealthToFightersAndAllies()
    {
        FrontierState plain = Front();
        FrontierState guild = Front((ResearchKind.Ramparts, 3));
        Ally ally = new(1, new HeroLook("Brune", 0, 0, 0, 0), HeroRole.Mage, 1, 60);
        Assert.Equal(Vitality.MaxHp(plain, FighterKind.Hero) * 1.15, Vitality.MaxHp(guild, FighterKind.Hero), 6);
        Assert.Equal(Vitality.MaxHp(plain, FighterKind.Second) * 1.15, Vitality.MaxHp(guild, FighterKind.Second), 6);
        Assert.Equal(Vitality.AllyMaxHp(plain, ally) * 1.15, Vitality.AllyMaxHp(guild, ally), 6);
    }

    [Fact]
    public void FinalAssaultHitsBossesHarder()
    {
        Assert.Equal(100, Blow(Front(), false), 6);
        Assert.Equal(100, Blow(Front((ResearchKind.FinalAssault, 3)), false), 6);
        Assert.Equal(Blow(Front(), true) * 1.18, Blow(Front((ResearchKind.FinalAssault, 3)), true), 6);
    }

    [Fact]
    public void CaravansAddGold()
    {
        FrontierState plain = Front();
        FrontierState guild = Front((ResearchKind.Caravans, 5));
        Assert.Equal(Economy.GoldPerSecond(plain) * 1.2, Economy.GoldPerSecond(guild), 6);
        Assert.Equal(Economy.KillGold(plain, true) * 1.2, Economy.KillGold(guild, true), 6);
    }

    [Fact]
    public void SchoolsAddExperience()
    {
        FrontierState plain = Front();
        FrontierState guild = Front((ResearchKind.Schools, 4));
        Experience.Gain(plain, FighterKind.Hero, 0.01, 40, new EventLog());
        Experience.Gain(guild, FighterKind.Hero, 0.01, 40, new EventLog());
        Assert.Equal(plain.Xp[0] * 1.2, guild.Xp[0], 6);
    }

    [Fact]
    public void GuildMarketLowersGearPrices()
    {
        FrontierState plain = Front();
        FrontierState guild = Front((ResearchKind.GuildMarket, 3));
        Assert.Equal(GearPrices.Offer(plain, 60, 1) * 0.85, GearPrices.Offer(guild, 60, 1), 6);
        Assert.Equal(GearPrices.Upgrade(plain, 40, 2, 3, 2) * 0.85, GearPrices.Upgrade(guild, 40, 2, 3, 2), 6);
    }

    [Fact]
    public void SanctuaryRechargesSkillsFaster()
    {
        FrontierState plain = Front();
        FrontierState guild = Front((ResearchKind.Sanctuary, 3));
        Assert.Equal(Economy.SkillCooldown(plain, SkillKind.Fury) / 1.12, Economy.SkillCooldown(guild, SkillKind.Fury), 6);
    }

    [Fact]
    public void ServerSideEffectsAreShownFromTheLevels()
    {
        ResearchView view = View((ResearchKind.Veterans, 2), (ResearchKind.HeavyConvoys, 3), (ResearchKind.Messengers, 3),
            (ResearchKind.Hospitality, 1), (ResearchKind.WarCry, 5));
        Assert.Equal(50, GuildResearch.AllyMinutes(view));
        Assert.Equal(95, GuildResearch.ConvoyGold(view));
        Assert.Equal(150, GuildResearch.CooldownMinutes(view));
        Assert.Equal(4, GuildResearch.FrontMax(view));
        Assert.Equal(50, GuildResearch.Effect(view, ResearchKind.WarCry), 6);
    }

    [Fact]
    public void LevelsAreBoundedByTheTree()
    {
        ResearchView view = View((ResearchKind.Hospitality, 7), (ResearchKind.CommonForge, -2));
        Assert.Equal(1, GuildResearch.Level(view, ResearchKind.Hospitality));
        Assert.Equal(0, GuildResearch.Level(view, ResearchKind.CommonForge));
        Assert.Equal(0, new ResearchView([2]).Level(ResearchKind.Hospitality));
    }

    [Fact]
    public void AFinishedResearchIsAnnouncedOnlyInsideTheSameGuild()
    {
        FrontierGame game = new(new FrontierState(), new System.Random(1));
        game.Study(View((ResearchKind.CommonForge, 1)));
        Assert.Empty(game.Events.Drain().OfType<ResearchDone>());
        game.Study(View((ResearchKind.CommonForge, 3), (ResearchKind.Caravans, 1)));
        Assert.Equal(
            new[] { new ResearchDone(ResearchKind.CommonForge, 2), new ResearchDone(ResearchKind.CommonForge, 3), new ResearchDone(ResearchKind.Caravans, 1) },
            game.Events.Drain().OfType<ResearchDone>());
        game.Study(null);
        Assert.Equal(1, GuildResearch.DamageFactor(game.State));
        game.Study(View((ResearchKind.CommonForge, 5)));
        game.Study(View((ResearchKind.CommonForge, 5), (ResearchKind.Schools, 1)), false);
        Assert.Empty(game.Events.Drain().OfType<ResearchDone>());
        Assert.Equal(1.2, GuildResearch.DamageFactor(game.State), 6);
        Assert.All(Researches.All, research => Assert.True(ArtFont.Covers($"{research.Name} niveau {research.MaxLevel}"), research.Name));
        Assert.True(ArtFont.Covers("RECHERCHE TERMINÉE"));
    }

    [Fact]
    public void CostsAndDurationsGrowWithLevelsAndMembers()
    {
        Assert.Equal(15, Researches.Cost(ResearchKind.CommonForge, 1, 1));
        Assert.Equal(24, Researches.Cost(ResearchKind.CommonForge, 2, 5));
        Assert.Equal(99, Researches.Cost(ResearchKind.CommonForge, 5, 3));
        Assert.Equal(72, Researches.Cost(ResearchKind.Caravans, 2, 15));
        Assert.Equal(30, Researches.Cost(ResearchKind.CommonForge, 1, 10));
        Assert.Equal(2, Researches.Hours(ResearchKind.WarCry, 1), 6);
        Assert.Equal(45, Researches.Hours(ResearchKind.FinalAssault, 3), 6);
        Assert.Equal(ResearchKind.Ramparts, Researches.Prerequisite(ResearchKind.Veterans));
        Assert.Null(Researches.Prerequisite(ResearchKind.Caravans));
        Assert.Equal(ResearchBranch.Rally, Researches.Of(ResearchKind.Hospitality).Branch);
        Assert.Equal("2 h", ResearchText.Hours(2));
        Assert.Equal("4 h 30", ResearchText.Hours(4.5));
        Assert.Equal("1 j 21 h", ResearchText.Hours(45));
        Assert.Equal(2453, TotalCost(5));
        Assert.Equal(4885, TotalCost(10));
        Assert.Equal(9763, TotalCost(20));
        Assert.Equal(587.25, TotalHours(), 6);
    }

    private static int TotalCost(int members) =>
        Researches.All.Sum(research => Enumerable.Range(1, research.MaxLevel).Sum(level => Researches.Cost(research.Kind, level, members)));

    private static double TotalHours() =>
        Researches.All.Sum(research => Enumerable.Range(1, research.MaxLevel).Sum(level => Researches.Hours(research.Kind, level)));

    private static double Blow(FrontierState state, bool boss)
    {
        state.Monster = new Monster(1, 0, boss, state.Stage, 1e12, 0);
        Battle.Damage(state, 100, HitSource.Hero, new EventLog());
        return 1e12 - state.Monster.Hp;
    }

    private static FrontierState Front(params (ResearchKind Kind, int Level)[] levels)
    {
        FrontierState state = new() { Stage = 40, MaxStage = 40, BestStage = 40, Fighters = [30, 20, 10], Role = HeroRole.Warrior };
        state.Built[0] = BuildingKind.Forge;
        state.Levels[0] = 5;
        state.Research = levels.Length == 0 ? null : View(levels);
        return state;
    }

    private static ResearchView View(params (ResearchKind Kind, int Level)[] levels)
    {
        int[] values = new int[Researches.All.Count];
        foreach ((ResearchKind kind, int level) in levels)
        {
            values[(int)kind] = level;
        }

        return new ResearchView(values);
    }
}
