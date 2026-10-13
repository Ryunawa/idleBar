using System.Text.Json;
using IdleBar.Cloud;
using IdleBar.Frontier;
using IdleBar.Online;
using Xunit;

namespace IdleBar.Tests;

public sealed class SocialDataTests
{
    private const string Reply = """
        {
          "status": "ok",
          "server_time": "2026-10-13T18:00:00+00:00",
          "guild": {
            "name": "Les Épées", "code": "K7PQ2Z", "leader": true,
            "members": [
              { "id": 4, "name": "Aldric", "role": "tank", "skin": 2, "hair": 1, "hair_color": 2, "outfit": 1, "level": 120,
                "best_stage": 87, "leader": true, "me": true, "away_seconds": 3.5, "heroes": 0, "convoy_in": 0 },
              { "id": 9, "name": "Brune", "role": "mage", "skin": 1, "hair": 1, "hair_color": 1, "outfit": 1, "level": 95,
                "best_stage": 70, "leader": false, "me": false, "away_seconds": 7200.25, "heroes": 2, "convoy_in": 1745.5 }
            ]
          },
          "rejoin_in": 0,
          "help": {
            "hero_ready_in": 0, "convoy_ready_in": 12000.5,
            "allies": [
              { "id": 31, "name": "Brune", "role": "mage", "skin": 1, "hair": 1, "hair_color": 1, "outfit": 1, "ratio": 0.8, "ends_in": 1500.75 }
            ],
            "boosts": [ { "source": "convoy", "gold": 50, "damage": 0, "ends_in": 900 } ]
          },
          "titan": {
            "kind": 2, "hp": 120000, "damage": 45000, "fallen": false, "ends_in": 300000, "mine": 1290,
            "claimable": { "percent": 50, "hours": 4 },
            "ranking": [ { "rank": 1, "name": "Les Épées", "monsters": 1290, "mine": true } ]
          }
        }
        """;

    [Fact]
    public void TheServerReplyIsRead()
    {
        SocialData? data = JsonSerializer.Deserialize<SocialData>(Reply, SupabaseJson.Options);
        Assert.NotNull(data);
        Assert.Equal(SocialData.Ok, data.Status);
        Assert.Equal("K7PQ2Z", data.Guild?.Code);
        Assert.Equal(HeroRole.Mage, data.Guild?.Members[1].Kind);
        Assert.Equal(1745.5, data.Guild?.Members[1].ConvoyIn);
        Assert.Equal(new TitanReward(50, 4), data.Titan?.Claimable);
        Assert.True(data.Titan?.Ranking[0].Mine);

        Ally ally = data.Help!.Allies[0].ToAlly();
        Assert.Equal(31, ally.Id);
        Assert.Equal(HeroRole.Mage, ally.Role);
        Assert.Equal(new HeroLook("Brune", 1, 1, 1, 1), ally.Look);
        Assert.Equal(1500.75, ally.Left);
        Assert.Equal(50, data.Help.Boosts[0].ToBoost().Gold);
    }

    [Fact]
    public void AReplyWithoutGuildIsRead()
    {
        const string lonely = """{ "status": "cooldown", "server_time": "2026-10-13T18:00:00+00:00", "guild": null, "rejoin_in": 1800.5 }""";
        SocialData? data = JsonSerializer.Deserialize<SocialData>(lonely, SupabaseJson.Options);
        Assert.Equal(SocialData.Cooldown, data?.Status);
        Assert.Null(data?.Guild);
        Assert.Null(data?.Titan);
        Assert.Equal(1800.5, data?.RejoinIn);
    }
}
