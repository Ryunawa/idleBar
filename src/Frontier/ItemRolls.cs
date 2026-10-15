using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public static class ItemRolls
{
    public const double Growth = 1.019;
    public const double Scale = 0.22;
    public const double SecondShare = 0.6;
    public const int MaxLevel = 1000;
    public const int LevelsBelow = 5;
    public const int LevelsAbove = 3;
    public const int LootChance = 30;

    public static IReadOnlyList<(double Min, double Max)> Bands { get; } = [(0.90, 1.10), (1.01, 1.23), (1.13, 1.38), (1.26, 1.55)];

    public static IReadOnlyList<int> BossOdds { get; } = [62, 28, 8, 2];

    public static IReadOnlyList<int> ShopOdds { get; } = [55, 32, 10, 3];

    public static double Value(double statBase, int rarity, int level, double roll)
    {
        (double min, double max) = Bands[Math.Clamp(rarity, 0, Bands.Count - 1)];
        return statBase * Scale * Math.Pow(Growth, Math.Clamp(level, 1, MaxLevel) - 1) * (min + (max - min) * Math.Clamp(roll, 0, 1));
    }

    public static double Centre(double statBase, int rarity, int level) => Value(statBase, rarity, level, 0.5);

    public static double Value(string stat, int rarity, int level, double roll, bool second = false) =>
        Value(ItemStats.Base(stat) * (second ? SecondShare : 1), rarity, level, roll);

    public static int Level(int reference, Random random) => Math.Max(1, reference + random.Next(-LevelsBelow, LevelsAbove + 1));

    public static int Rarity(IReadOnlyList<int> odds, double roll)
    {
        double total = 0;
        for (int rank = 0; rank < odds.Count; rank++)
        {
            total += odds[rank] / 100.0;
            if (roll < total)
            {
                return rank;
            }
        }

        return odds.Count - 1;
    }

    public static string OddsLine(IEnumerable<(string Rarity, double Percent)> odds) => "Chances : " + Split(odds);

    public static string OddsLine(IReadOnlyList<int> odds) => "Chances : " + Split(odds);

    public static string Split(IReadOnlyList<int> odds) => Split(odds.Select((percent, rank) => (Items.RarityId(rank), (double)percent)));

    public static string Split(IEnumerable<(string Rarity, double Percent)> odds) =>
        string.Join(" · ", odds.OrderBy(entry => Items.Rank(entry.Rarity)).Select(entry => $"{Items.RarityName(Items.Rank(entry.Rarity))} {Items.Percent(entry.Percent)}"));

    public static string BandLine() =>
        string.Join(", ", Bands.Select((band, rank) => $"{Items.Number(band.Min * 100)} à {Items.Percent(band.Max * 100)} pour une {Items.RarityName(rank)}"));
}
