using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using IdleBar.Cloud;
using IdleBar.Frontier;
using IdleBar.Online;
using Xunit;

namespace IdleBar.Tests;

public sealed class ResearchServerTests
{
    private static readonly string Script = Read("48_guild_research.sql");

    private const string Reply = """
        {
          "status": "ok", "server_time": "2026-10-15T18:00:00+00:00", "rejoin_in": 0, "given": 9,
          "guild": { "name": "Les Sceaux", "code": "K7PQ2Z", "leader": true, "members": [] },
          "titan": { "kind": 1, "hp": 100, "damage": 100, "fallen": true, "ends_in": 10, "mine": 600, "ranking": [],
            "claimable": { "percent": 25, "hours": 4, "seals": 10 },
            "tiers": [ { "points": 500, "percent": 25, "hours": 4, "seals": 10 } ] },
          "research": {
            "seals": 12, "members": 7,
            "nodes": [
              { "id": "common_forge", "level": 2, "queued": 1, "funded": 5, "cost": 55, "hours": 6.75, "frozen": true },
              { "id": "hospitality", "level": 1, "queued": 0, "funded": 0, "cost": null, "hours": null },
              { "id": "castle", "level": 3, "queued": 0, "funded": 0, "cost": 1, "hours": 1 }
            ],
            "queue": [
              { "node": "common_forge", "level": 3, "starts_in": 0, "ends_in": 3600.5, "hours": 4.5 },
              { "node": "caravans", "level": 1, "starts_in": 3600.5, "ends_in": 10800.5, "hours": 2 }
            ]
          }
        }
        """;

    [Fact]
    public void TheTreeMatchesTheServer()
    {
        MatchCollection nodes = Regex.Matches(Script, @"\('(\w+)', '(\w+)', (\d), (\d), (\d+), ([\d.]+), ([\d.]+)\)");
        Assert.Equal(Researches.All.Count, nodes.Count);
        foreach (Match node in nodes)
        {
            ResearchInfo research = Researches.Of(Researches.Parse(node.Groups[1].Value)!.Value);
            Assert.Equal(node.Groups[2].Value, research.Branch.ToString().ToLowerInvariant());
            Assert.Equal(int.Parse(node.Groups[3].Value, CultureInfo.InvariantCulture), Researches.Position(research.Kind));
            Assert.Equal(int.Parse(node.Groups[4].Value, CultureInfo.InvariantCulture), research.MaxLevel);
            Assert.Equal(int.Parse(node.Groups[5].Value, CultureInfo.InvariantCulture), research.Cost);
            Assert.Equal(Number(node.Groups[6].Value), research.Hours);
            Assert.Equal(Number(node.Groups[7].Value), research.Effect);
        }

        Assert.Equal(Researches.SealsPerHour, Setting("seal_bosses_per_hour"));
        Assert.Equal(Researches.CostGrowth, Setting("research_growth"));
        Assert.Equal(Researches.TimeGrowth, Setting("research_time_growth"));
        Assert.Equal(Researches.MembersStep, Setting("research_members_step"));
        Assert.Contains($"(500, {Researches.TitanSeals[0]}), (3000, {Researches.TitanSeals[1]}), (10000, {Researches.TitanSeals[2]})", Script);
        Assert.DoesNotContain("min_client_version", Script);
    }

    [Fact]
    public void TheResearchReplyIsRead()
    {
        SocialData data = JsonSerializer.Deserialize<SocialData>(Reply, SupabaseJson.Options)!;
        ResearchData research = data.Research!;
        Assert.Equal(9, data.Given);
        Assert.Equal(12, research.Seals);
        Assert.Equal(7, research.Members);
        Assert.Equal(new TitanReward(25, 4, 10), data.Titan!.Claimable);
        Assert.Equal(10, data.Titan.Tiers![0].Seals);
        Assert.Equal(50, research.Node(ResearchKind.CommonForge)!.Missing);
        Assert.Null(research.Node(ResearchKind.Hospitality)!.Cost);
        Assert.True(research.Node(ResearchKind.CommonForge)!.Frozen);
        Assert.False(research.Node(ResearchKind.Hospitality)!.Frozen);
        Assert.Equal(0, research.Node(ResearchKind.Hospitality)!.Missing);
        Assert.Equal(ResearchKind.CommonForge, research.Running?.Kind);
        Assert.Equal(0, research.Position(ResearchKind.CommonForge));
        Assert.Equal(1, research.Position(ResearchKind.Caravans));
        Assert.Equal(ResearchKind.Caravans, Assert.Single(research.Waiting).Kind);
        Assert.Equal(0, research.Position(ResearchKind.Schools));
        ResearchView? view = data.ResearchLevels;
        Assert.NotNull(view);
        Assert.Equal(2, view.Level(ResearchKind.CommonForge));
        Assert.Equal(1, view.Level(ResearchKind.Hospitality));
        Assert.Equal(0, view.Level(ResearchKind.Caravans));
        Assert.Equal(4, GuildResearch.FrontMax(view));
        Assert.Null((data with { Guild = null }).ResearchLevels);
    }

    [Fact]
    public void AnOldServerReplyHasNoResearch()
    {
        const string old = """{ "status": "ok", "server_time": "2026-10-13T18:00:00+00:00", "guild": null, "rejoin_in": 0 }""";
        SocialData data = JsonSerializer.Deserialize<SocialData>(old, SupabaseJson.Options)!;
        Assert.Null(data.Research);
        Assert.Null(data.ResearchLevels);
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
