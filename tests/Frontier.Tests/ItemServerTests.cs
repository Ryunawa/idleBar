using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IdleBar.Frontier;
using Xunit;

namespace IdleBar.Tests;

public sealed class ItemServerTests
{
    private static readonly string Script = Read("43_item_variety.sql");
    private static readonly string Slots = Read("44_more_slots.sql");

    [Fact]
    public void TheOddsMatchTheServer()
    {
        MatchCollection odds = Regex.Matches(Script, @"\('(boss|shop)', '(\w+)', (\d+)\)");
        Assert.Equal(8, odds.Count);
        foreach (Match entry in odds)
        {
            int rank = Items.Rank(entry.Groups[2].Value);
            int percent = int.Parse(entry.Groups[3].Value, CultureInfo.InvariantCulture);
            Assert.Equal(entry.Groups[1].Value == "boss" ? ItemRolls.BossOdds[rank] : ItemRolls.ShopOdds[rank], percent);
        }

        Match chance = Regex.Match(Read("33_loot.sql"), @"loot_chance numeric not null default ([\d.]+)");
        Assert.Equal(ItemRolls.LootChance, Number(chance.Groups[1].Value) * 100, 6);
    }

    [Fact]
    public void TheSlotsAndTheirStatsMatchTheServer()
    {
        MatchCollection slots = Regex.Matches(Read("33_loot.sql") + Slots, @"\('(\w+)', (\d)\)");
        Assert.Equal(Items.Slots.OrderBy(slot => slot), slots.Select(slot => slot.Groups[1].Value).OrderBy(slot => slot));
        MatchCollection stats = Regex.Matches(Slots, @"\('(\w+)', ([\d.]+), '\{([\w,]+)\}', '\{([\w,]+)\}'\)");
        Assert.Equal(Items.Stats.Count, stats.Count);
        foreach (Match stat in stats)
        {
            string id = stat.Groups[1].Value;
            Assert.Equal(ItemStats.Base(id), Number(stat.Groups[2].Value));
            Assert.Equal(ItemStats.SlotsOf(id).OrderBy(slot => slot), stat.Groups[3].Value.Split(',').OrderBy(slot => slot));
            Assert.Equal(ItemStats.RolesOf(id).Select(role => Roles.Of(role).Id).OrderBy(role => role), stat.Groups[4].Value.Split(',').OrderBy(role => role));
        }

        Assert.Equal(ItemRolls.Scale, Number(Regex.Match(Slots, @"rescale_values\(([\d.]+)\)").Groups[1].Value));
        Assert.Contains("min_client_version = '0.9.0'", Slots);
    }

    [Fact]
    public void TheValueFormulaMatchesTheServer()
    {
        Assert.Equal(ItemRolls.Growth, Setting("item_growth"));
        Assert.Equal(ItemRolls.SecondShare, Setting("item_second_share"));
        Assert.Equal(ItemRolls.LevelsBelow, Setting("item_levels_below"));
        Assert.Equal(ItemRolls.LevelsAbove, Setting("item_levels_above"));
        MatchCollection bands = Regex.Matches(Script, @"set roll_min = ([\d.]+), roll_max = ([\d.]+), factor = [\d.]+ where id = '(\w+)'");
        Assert.Equal(ItemRolls.Bands.Count, bands.Count);
        foreach (Match band in bands)
        {
            int rank = Items.Rank(band.Groups[3].Value);
            Assert.Equal(ItemRolls.Bands[rank], (Number(band.Groups[1].Value), Number(band.Groups[2].Value)));
        }

        Match damage = Regex.Match(Script, @"\('damage', ([\d.]+),");
        Assert.Equal(4, Number(damage.Groups[1].Value));
        Assert.Equal(Items.Stats.Count, Regex.Matches(Script, @"\('(\w+)', [\d.]+, '\{").Count);
        Assert.All(Items.Stats, stat => Assert.Contains($"('{stat}', ", Script));
    }

    private static double Setting(string name) =>
        Number(Regex.Match(Script, $@"{name} \w+ not null default ([\d.]+)").Groups[1].Value);

    private static double Number(string text) => double.Parse(text, CultureInfo.InvariantCulture);

    private static string Read(string script)
    {
        DirectoryInfo? folder = new(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "supabase", script)))
        {
            folder = folder.Parent;
        }

        Assert.NotNull(folder);
        return File.ReadAllText(Path.Combine(folder.FullName, "supabase", script));
    }
}
