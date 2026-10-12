using System.Collections.Generic;

namespace IdleBar.Frontier;

public sealed class FrontierState
{
    public const int FighterCount = 3;
    public const int BuildingCount = 7;
    public const int SkillCount = 3;

    public double Time { get; set; }

    public double Gold { get; set; }

    public double LifetimeGold { get; set; }

    public int Souls { get; set; }

    public int Retreats { get; set; }

    public int Stage { get; set; } = 1;

    public int MaxStage { get; set; } = 1;

    public int BestStage { get; set; } = 1;

    public int Kills { get; set; }

    public int[] Fighters { get; set; } = [1, 0, 0];

    public int[] Buildings { get; set; } = new int[BuildingCount];

    public double[] Cooldowns { get; set; } = new double[SkillCount];

    public double[] Timers { get; } = new double[FighterCount];

    public double FuryLeft { get; set; }

    public double FrenzyLeft { get; set; }

    public int Supply { get; set; }

    public double SupplyLeft { get; set; }

    public List<Convoy> Convoys { get; } = [];

    public double ConvoyTimer { get; set; } = 2;

    public long ConvoySequence { get; set; }

    public double? Chicken { get; set; }

    public double ChickenTimer { get; set; } = Balance.ChickenFirst;

    public Monster? Monster { get; set; }

    public long MonsterSequence { get; set; }

    public double RespawnIn { get; set; } = 0.2;

    public bool IntendantUnlocked { get; set; }

    public bool IntendantOn { get; set; }

    public HashSet<string> Achievements { get; } = [];

    public long MonstersKilled { get; set; }

    public long BossesKilled { get; set; }

    public long Chickens { get; set; }

    public long Clicks { get; set; }

    public long SkillsUsed { get; set; }

    public int Level(FighterKind kind) => Fighters[(int)kind];

    public int Level(BuildingKind kind) => Buildings[(int)kind];

    public void Earn(double gold)
    {
        Gold += gold;
        LifetimeGold += gold;
    }
}
