using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace IdleBar.Tests;

public sealed class PacingTests
{
    private const double Minute = 60;
    private const double Hour = 3_600;
    private const double PlayedPerDay = 8 * Hour;
    private const double AwayPerDay = 16 * Hour;

    private readonly ITestOutputHelper _output;

    public PacingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TheFirstHoursFollowTheTargets()
    {
        IdlePlayer player = new(7);
        player.Play(6 * Hour);
        Report(player);

        Assert.InRange(At(player, "premier-batiment"), 0, 1 * Minute);
        Assert.InRange(At(player, "niveau-11"), 3 * Minute, 5 * Minute);
        Assert.InRange(At(player, "repli-possible-1"), 2 * Hour, 4 * Hour);
    }

    [Fact]
    public void TwoWeeksOfPlayReachTheLateGame()
    {
        IdlePlayer player = new(11);
        for (int day = 0; day < 14; day++)
        {
            player.Play(PlayedPerDay);
            player.Away(AwayPerDay);
        }

        Report(player);
        double firstRetreat = At(player, "repli-possible-1");
        double secondRun = At(player, "repli-possible-2") - At(player, "repli-1");
        Assert.InRange(secondRun, firstRetreat * 0.2, firstRetreat * 0.7);
        Assert.InRange(At(player, "niveau-150"), 7 * PlayedPerDay, 14 * PlayedPerDay);
        Assert.InRange(At(player, "palier-100"), 2 * PlayedPerDay, 14 * PlayedPerDay);
    }

    private static double At(IdlePlayer player, string milestone) =>
        player.Milestones.TryGetValue(milestone, out double clock) ? clock : double.PositiveInfinity;

    private void Report(IdlePlayer player)
    {
        foreach (KeyValuePair<string, double> milestone in player.Milestones.OrderBy(pair => pair.Value))
        {
            _output.WriteLine($"{milestone.Key,-22} {milestone.Value / Hour,8:0.00} h");
        }

        foreach (string note in player.Notes)
        {
            _output.WriteLine(note);
        }

        _output.WriteLine($"stage {player.Game.State.Stage} best {player.Game.State.BestStage} souls {player.Game.State.Souls} retreats {player.Game.State.Retreats}");
    }
}
