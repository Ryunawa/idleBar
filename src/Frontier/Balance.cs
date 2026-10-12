namespace IdleBar.Frontier;

public static class Balance
{
    public const double Step = 0.05;
    public const double LongGap = 5;

    public const int MonstersPerStage = 5;
    public const int BossEvery = 10;
    public const double BossSeconds = 30;
    public const double BossHpFactor = 8;
    public const double BossGoldFactor = 6;
    public const double MonsterHpBase = 24;
    public const double KillGoldRatio = 0.35;
    public const double KillGoldFlat = 2;
    public const double RespawnSeconds = 0.35;
    public const double ApproachSeconds = 0.7;
    public const double BossApproachSeconds = 1.4;
    public const double BossRetrySeconds = 1.5;
    public const double ChargeShare = 0.9;

    public const double FighterCostGrowth = 1.11;
    public const double BuildingCostGrowth = 1.17;
    public const double TierFactor = 2;
    public const double MilestoneFactor = 1.5;
    public const int MilestoneLevels = 10;
    public const double ClickShare = 0.35;

    public const double SoulBonus = 0.025;
    public const double AchievementBonus = 0.02;
    public const int RetreatStage = 50;
    public const double SoulDivisor = 10;
    public const double SoulExponent = 2;

    public const double ConvoyEvery = 9;
    public const double ConvoyTravel = 10;
    public const double SupplySeconds = 25;
    public const int SupplyMax = 5;
    public const double SupplyBonus = 0.05;

    public const double ChickenFirst = 90;
    public const double ChickenMin = 180;
    public const double ChickenMax = 480;
    public const double ChickenFlight = 14;
    public const double FrenzySeconds = 20;
    public const double FrenzyGold = 7;

    public const double SmiteHits = 25;
    public const double FuryFactor = 2;
    public const double GoldRainSeconds = 30;
    public const double GoldRainKills = 10;

    public const double AbsenceCapSeconds = 8 * 3600;
    public const double IntendantSeconds = 0.5;
    public const double IntendantReach = 20;
    public const double AchievementSeconds = 1;

    public const double MillAbsenceSeconds = 600;
    public const double AbsenceMaxSeconds = 24 * 3600;
    public const double ForgeBonus = 0.01;
    public const double MarketBonus = 0.02;
    public const double TowerBonus = 0.03;

    public static readonly int[] Tiers = [10, 25, 50, 100, 250];

    public static readonly GrowthSegment[] HpCurve = [new(10, 1.33), new(50, 1.34), new(100, 1.19), new(int.MaxValue, 1.13)];
}
