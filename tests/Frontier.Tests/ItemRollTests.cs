using System;
using System.Linq;
using System.Text.Json;
using IdleBar.Cloud;
using IdleBar.Frontier;
using IdleBar.Online;
using Xunit;

namespace IdleBar.Tests;

public sealed class ItemRollTests
{
    private static readonly double[] Bases = [2, 3, 4, 5, 6, 10];

    [Fact]
    public void LevelOutweighsRarity()
    {
        foreach (double statBase in Bases)
        {
            Assert.True(ItemRolls.Value(statBase, 1, 60, 0) > ItemRolls.Value(statBase, 3, 30, 1));
            Assert.True(ItemRolls.Value(statBase, 2, 45, 0.5) > ItemRolls.Value(statBase, 3, 30, 0.5));
            for (int level = 1; level <= 300; level += 7)
            {
                Assert.True(ItemRolls.Value(statBase, 3, level, 0) > ItemRolls.Value(statBase, 1, level, 1));
                Assert.True(ItemRolls.Value(statBase, 2, level, 0) > ItemRolls.Value(statBase, 0, level, 1));
                Assert.True(ItemRolls.Value(statBase, 0, level + 1, 0.5) > ItemRolls.Value(statBase, 0, level, 0.5));
                for (int rarity = 1; rarity < ItemRolls.Bands.Count; rarity++)
                {
                    Assert.True(ItemRolls.Centre(statBase, rarity, level) > ItemRolls.Centre(statBase, rarity - 1, level));
                }
            }
        }
    }

    [Fact]
    public void EachRarityStepIsWorthFiveToEightLevels()
    {
        for (int rarity = 1; rarity < ItemRolls.Bands.Count; rarity++)
        {
            double step = Math.Log(ItemRolls.Centre(1, rarity, 50) / ItemRolls.Centre(1, rarity - 1, 50)) / Math.Log(ItemRolls.Growth);
            Assert.InRange(step, 5, 8);
        }

        double legendary = Math.Log(ItemRolls.Centre(1, 3, 50) / ItemRolls.Centre(1, 0, 50)) / Math.Log(ItemRolls.Growth);
        Assert.InRange(legendary, 15, 24);
        Assert.All(ItemRolls.Bands, band => Assert.InRange((band.Max - band.Min) / (band.Max + band.Min), 0.09, 0.15));
    }

    [Fact]
    public void ItemLevelsStayCloseToTheBestFighter()
    {
        Random random = new(3);
        int[] levels = [.. Enumerable.Range(0, 500).Select(_ => ItemRolls.Level(40, random))];
        Assert.Equal(35, levels.Min());
        Assert.Equal(43, levels.Max());
        Assert.All(Enumerable.Range(0, 50), _ => Assert.True(ItemRolls.Level(2, random) >= 1));
    }

    [Fact]
    public void OddsAreWholePercentagesShownInFrench()
    {
        Assert.Equal(100, ItemRolls.BossOdds.Sum());
        Assert.Equal(100, ItemRolls.ShopOdds.Sum());
        Assert.Equal("Chances : commune 62 % · rare 28 % · épique 8 % · légendaire 2 %", ItemRolls.OddsLine(ItemRolls.BossOdds));
        Assert.Equal(2, ItemRolls.Rarity(ItemRolls.BossOdds, 0.91));
        Assert.Equal(3, ItemRolls.Rarity(ItemRolls.ShopOdds, 0.995));
    }

    [Fact]
    public void TheMarketShowsTheOddsTheServerSends()
    {
        const string Reply = """
            { "refresh_in": 60, "odds": [ { "rarity": "rare", "percent": 32 }, { "rarity": "common", "percent": 55 },
              { "rarity": "epic", "percent": 10 }, { "rarity": "legendary", "percent": 2.5 } ], "offers": [] }
            """;
        ShopData? shop = JsonSerializer.Deserialize<ShopData>(Reply, SupabaseJson.Options);
        Assert.Equal("Chances : commune 55 % · rare 32 % · épique 10 % · légendaire 2,5 %", shop?.OddsLine);
        Assert.Null(new ShopData(0, []).OddsLine);
    }

    [Fact]
    public void ABoughtItemRefundsAtTheStageWhereItWasSold()
    {
        ItemData bought = new(1, Items.Helm, "rare", 30, Items.Damage, 5, null, null, 6, Items.ShopSource, false, Stage: 80);
        ItemData older = bought with { Stage = 0 };
        Assert.Equal(80, bought.PriceStage);
        Assert.Equal(30, older.PriceStage);
        Assert.True(bought.Reached(30));
        Assert.False(bought.Reached(29));
        Assert.Equal("Niveau 30 requis", Items.Required(bought.Level));
    }
}
