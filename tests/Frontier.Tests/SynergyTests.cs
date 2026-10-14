using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;
using Xunit;
using Xunit.Abstractions;

namespace IdleBar.Tests;

public sealed class SynergyTests
{
    private const int Days = 4;
    private const double Played = 8 * 3600;
    private const double Away = 16 * 3600;

    private readonly ITestOutputHelper _output;

    public SynergyTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void ABalancedTeamOutpacesEveryMonoClassTeam()
    {
        IdlePlayer balanced = Run(HeroRole.Support, HeroRole.Warrior, HeroRole.Archer);
        foreach (HeroRole role in Roles.All.Select(info => info.Role))
        {
            IdlePlayer mono = Run(role, role, role);
            Assert.True(Score(balanced) > Score(mono), $"{role} : {Score(mono)} contre {Score(balanced)}");
        }
    }

    [Fact]
    public void DamageDealersAloneFallFarMoreThanAGuardedTeam()
    {
        IdlePlayer guarded = Run(HeroRole.Warrior, HeroRole.Tank, HeroRole.Support);
        IdlePlayer fragile = Run(HeroRole.Warrior, HeroRole.Archer, HeroRole.Mage);
        Assert.True(Falls(fragile) > Falls(guarded) * 3, $"{Falls(fragile)} chutes contre {Falls(guarded)}");
    }

    private static int Score(IdlePlayer player) => player.Game.State.BestStage * 1000 + player.Game.State.Souls;

    private static int Falls(IdlePlayer player) => player.Counts.Where(pair => pair.Key.StartsWith("tombe")).Sum(pair => pair.Value);

    private IdlePlayer Run(HeroRole hero, HeroRole first, HeroRole second)
    {
        IdlePlayer player = new(5, hero, first, second);
        for (int day = 0; day < Days; day++)
        {
            player.Play(Played);
            player.Away(Away);
        }

        FrontierState state = player.Game.State;
        _output.WriteLine($"{hero}/{first}/{second} : niveau {state.BestStage}, {state.Souls} âmes, {Falls(player)} chutes, {player.Counts.GetValueOrDefault(nameof(Routed))} reculs, {player.Counts.GetValueOrDefault(nameof(BossFailed))} boss ratés");
        return player;
    }
}
