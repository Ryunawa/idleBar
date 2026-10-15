using System;
using System.Collections.Generic;

namespace IdleBar.Frontier;

public sealed class FrontierState
{
    public const int FighterCount = 3;
    public const int BuildingCount = 12;
    public const int PlotCount = 12;
    public const int SlotCount = 3;
    public const int NodeCount = 15;

    public double Time { get; set; }

    public double Gold { get; set; }

    public double LifetimeGold { get; set; }

    public int Souls { get; set; }

    public int Retreats { get; set; }

    public int Stage { get; set; } = 1;

    public int MaxStage { get; set; } = 1;

    public int BestStage { get; set; } = 1;

    public int Kills { get; set; }

    public bool BossHeld { get; set; }

    public HeroRole Role { get; set; }

    public int RoleRetreats { get; set; }

    public HeroLook? Hero { get; set; }

    public int[] Fighters { get; set; } = [1, 0, 0];

    public HeroRole?[] Companions { get; set; } = new HeroRole?[FighterCount - 1];

    public int Armor { get; set; }

    public BuildingKind?[] Built { get; set; } = new BuildingKind?[PlotCount];

    public int[] Levels { get; set; } = new int[PlotCount];

    public int[] Learned { get; } = new int[NodeCount];

    public SkillKind?[] Slots { get; } = new SkillKind?[SlotCount];

    public double[] Cooldowns { get; } = new double[NodeCount];

    public bool[] PlacedThisRun { get; } = new bool[NodeCount];

    public double[] Timers { get; } = new double[FighterCount];

    public double[] Health { get; } = [1, 1, 1];

    public double[] Downed { get; } = new double[FighterCount];

    public double[] CritMeters { get; } = new double[FighterCount];

    public double[] ActiveTimers { get; } = [0, Balance.ActiveStagger, 2 * Balance.ActiveStagger];

    public double[] Guards { get; } = new double[FighterCount];

    public double[] Xp { get; set; } = new double[FighterCount];

    public Gear[] Gears { get; } = [Gear.None, Gear.None, Gear.None];

    public GearLook[] Worn { get; } = [GearLook.None, GearLook.None, GearLook.None];

    public double FuryLeft { get; set; }

    public double StormLeft { get; set; }

    public double ShieldLeft { get; set; }

    public double HarvestLeft { get; set; }

    public double FrenzyLeft { get; set; }

    public List<double> Rations { get; } = [];

    public int Supply => Rations.Count;

    public List<Convoy> Convoys { get; } = [];

    public List<Ally> Allies { get; } = [];

    public List<Boost> Boosts { get; } = [];

    public TitanView? Titan { get; set; }

    public double ConvoyTimer { get; set; } = 2;

    public long ConvoySequence { get; set; }

    public double? Chicken { get; set; }

    public bool Lured { get; set; }

    public double ChickenTimer { get; set; } = Balance.ChickenFirst;

    public Monster? Monster { get; set; }

    public long MonsterSequence { get; set; }

    public double RespawnIn { get; set; } = 0.2;

    public HashSet<string> Achievements { get; } = [];

    public long MonstersKilled { get; set; }

    public long BossesKilled { get; set; }

    public long Chickens { get; set; }

    public long Clicks { get; set; }

    public long SkillsUsed { get; set; }

    public int Level(FighterKind kind) => Fighters[(int)kind];

    public HeroRole ClassOf(FighterKind kind) =>
        kind == FighterKind.Hero ? Role : Companions[(int)kind - 1] ?? Frontier.Fighters.Of(kind).Class;

    public int Level(BuildingKind kind) => PlotOf(kind) is int plot ? Levels[plot] : 0;

    public int? PlotOf(BuildingKind kind)
    {
        int plot = Array.IndexOf(Built, kind);
        return plot >= 0 ? plot : null;
    }

    public bool CanChangeRole => Hero is null || Retreats > RoleRetreats;

    public void Earn(double gold)
    {
        Gold += gold;
        LifetimeGold += gold;
    }
}
