using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Tests;

public sealed class IdlePlayer
{
    private const double ActiveSeconds = 600;
    private const double ActiveCheck = 10;
    private const double IdleCheck = 300;
    private const double ChickenChance = 0.3;
    private const double StallSeconds = 1_800;
    private const double RetreatShare = 0.1;

    private readonly Random _random;
    private readonly Dictionary<string, double> _milestones = [];
    private readonly List<string> _notes = [];
    private readonly Dictionary<string, int> _counts = [];
    private double _sinceCheck;
    private double _lastProgress;
    private int _lastMaxStage;

    public IdlePlayer(int seed, HeroRole role = HeroRole.Warrior, HeroRole? first = null, HeroRole? second = null)
    {
        _random = new Random(seed);
        Game = new FrontierGame(new FrontierState { Role = role }, new Random(seed));
        (HeroRole First, HeroRole Second) team = Shopper.Complement(role);
        Shopper = new Shopper(Game, first ?? team.First, second ?? team.Second, seed);
    }

    public FrontierGame Game { get; }

    public Shopper Shopper { get; }

    public double Clock { get; private set; }

    public IReadOnlyDictionary<string, double> Milestones => _milestones;

    public IReadOnlyList<string> Notes => _notes;

    public IReadOnlyDictionary<string, int> Counts => _counts;

    public void Play(double seconds)
    {
        for (double elapsed = 0; elapsed < seconds; elapsed += 1)
        {
            Game.Advance(1);
            Clock += 1;
            React();
            Note();
            _sinceCheck += 1;
            if (_sinceCheck >= (Clock < ActiveSeconds ? ActiveCheck : IdleCheck))
            {
                _sinceCheck = 0;
                CheckIn();
            }
        }
    }

    public int Idle(double seconds)
    {
        int lowest = Game.State.Stage;
        for (double elapsed = 0; elapsed < seconds; elapsed += 1)
        {
            Game.Advance(1);
            Clock += 1;
            React();
            lowest = Math.Min(lowest, Game.State.Stage);
        }

        return lowest;
    }

    public void Away(double seconds)
    {
        Game.Absent(seconds);
        Game.Events.Drain();
        _lastProgress = Clock;
        CheckIn();
    }

    private void React()
    {
        foreach (FrontierEvent frontierEvent in Game.Events.Drain())
        {
            if (frontierEvent is Fell or Routed or BossFailed)
            {
                string name = frontierEvent switch
                {
                    Fell fell => $"tombe-{fell.Kind}",
                    Routed { Boss: true } => "Routed-boss",
                    _ => frontierEvent.GetType().Name,
                };
                _counts[name] = _counts.GetValueOrDefault(name) + 1;
            }

            if (frontierEvent is ChickenAppeared && _random.NextDouble() < ChickenChance)
            {
                Game.CatchChicken();
            }

            if (frontierEvent is Killed { Boss: true })
            {
                Shopper.Loot();
            }
        }
    }

    private void CheckIn()
    {
        FrontierState state = Game.State;
        foreach (SkillInfo skill in Skills.All)
        {
            Game.UseSkill(skill.Kind);
        }

        Shopper.Recruit();
        Shopper.Restock(Clock);
        while (Shopper.BuyOne())
        {
        }

        Game.CallBoss();

        int souls = RetreatRules.Pending(state);
        bool stalled = Clock - _lastProgress >= StallSeconds;
        if (stalled && souls >= Math.Max(1, state.Souls * RetreatShare))
        {
            Mark($"repli-{state.Retreats + 1}");
            Game.Retreat();
            _lastMaxStage = 0;
            _lastProgress = Clock;
        }
    }

    private void Note()
    {
        FrontierState state = Game.State;
        if (state.MaxStage > _lastMaxStage)
        {
            _lastMaxStage = state.MaxStage;
            _lastProgress = Clock;
        }

        if (Array.Exists(state.Built, kind => kind is not null))
        {
            Mark("premier-batiment");
        }

        foreach (int stage in (int[])[11, 50, 100, 150, 200])
        {
            if (state.BestStage >= stage)
            {
                Mark($"niveau-{stage}");
            }
        }

        if (state.MaxStage >= Balance.RetreatStage)
        {
            Mark($"repli-possible-{state.Retreats + 1}");
        }

        if (Array.Exists(state.Levels, level => level >= 100))
        {
            Mark("palier-100");
        }
    }

    private void Mark(string milestone)
    {
        if (_milestones.TryAdd(milestone, Clock))
        {
            FrontierState state = Game.State;
            _notes.Add($"{milestone}: fighters {string.Join('/', state.Fighters)} gear {Shopper.Gear.Count} hero+{state.Gears[0].Damage:0.00} boss+{state.Gears[0].Boss:0.00} plunder {Economy.Plunder(state):0.00} routs {_counts.GetValueOrDefault(nameof(Routed))} falls {_counts.Where(pair => pair.Key.StartsWith("tombe")).Sum(pair => pair.Value)} buildings {string.Join('/', state.Built.Select(kind => kind?.ToString() ?? "-"))} levels {string.Join('/', state.Levels)} gps {Economy.BaseGoldPerSecond(state):0} dps {Economy.TotalDps(state):0} souls {state.Souls} lifetime {state.LifetimeGold:0.00e0} stage {state.MaxStage}");
        }
    }
}
