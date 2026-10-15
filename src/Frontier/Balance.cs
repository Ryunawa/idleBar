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

    public const double XpGrowth = 1.35;
    public const double OldLevelsPerLevel = 2.876;
    public const double BuildingCostGrowth = 1.25;
    public const double TierFactor = 2;
    public const double MilestoneFactor = 3.2;
    public const int MilestoneLevels = 10;
    public const double ClickShare = 0.35;

    public const double SoulBonus = 0.025;
    public const double AchievementBonus = 0.02;
    public const int RetreatStage = 50;
    public const double SoulScale = 5;
    public const double SoulGoldBase = 1_000_000;
    public const double SoulPower = 0.23;

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
    public const double SmiteInterval = 0.6;
    public const double FuryFactor = 2;
    public const double GoldRainSeconds = 30;
    public const double GoldRainKills = 10;

    public const double AbsenceCapSeconds = 8 * 3600;
    public const double AchievementSeconds = 1;

    public const double MillAbsenceSeconds = 600;
    public const double AbsenceMaxSeconds = 24 * 3600;
    public const double AbsenceFrontShare = 0.2;
    public const double ForgeBonus = 0.03;
    public const double MarketBonus = 0.02;
    public const double TowerBonus = 0.02;
    public const double PortalBonus = 0.005;
    public const double FarmChickenBonus = 0.01;
    public const double FarmChickenMax = 1;
    public const double BakeryBonus = 0.01;
    public const double BakeryMax = 1;
    public const double BarracksBonus = 0.02;
    public const double ChapelBonus = 0.02;
    public const double ChapelMax = 2;
    public const double TavernBonus = 0.03;
    public const double WorkshopDiscount = 0.005;
    public const double WorkshopMax = 0.5;
    public const double BelfrySeconds = 0.5;
    public const double BelfryMax = 30;

    public const double MonsterHit = 0.12;
    public const double MonsterStrikeSeconds = 1.5;
    public const double BossHit = 1.2;
    public const double BossStrikeSeconds = 2;
    public const double HitRampStages = 40;
    public const double HitRampFloor = 0.15;
    public const double FirstStrikeShare = 0.6;
    public const double MeleeWeight = 4;
    public const double RangedWeight = 1;
    public const double KillHeal = 0.03;
    public const double CriticalFactor = 2.5;
    public const double CompanionShare = 0.5;
    public const double RangerSpeed = 0.2;
    public const double MageBossBonus = 0.25;
    public const double WarriorIntimidation = 0.25;
    public const double SupportBlessing = 0.3;
    public const double GuardShare = 0.6;
    public const double GuardSeconds = 10;
    public const double StunHitFactor = 2;
    public const double StunSeconds = 3;
    public const int VolleyShots = 5;
    public const double BlastFactor = 2.5;
    public const double StunCooldown = 15;
    public const double GuardCooldown = 15;
    public const double VolleyCooldown = 20;
    public const double BlastCooldown = 10;
    public const double RaiseCooldown = 20;
    public const double PrayerHeal = 0.3;
    public const double ClericTouch = 0.03;
    public const double ActiveStagger = 3;
    public const double SynergyBonus = 0.1;
    public const double ArmorBonus = 0.1;
    public const double FrontToughness = 0.25;
    public const double LateToughness = 0.12;
    public const int ToughStages = 70;
    public const double BarracksHpBonus = 0.02;

    public const double XpRatio = 0.012;
    public const double XpFlat = 9;
    public const double XpSmall = 0.05;
    public const double XpActive = 0.4;
    public const double XpTaken = 1;
    public const double XpBossTaken = 3;
    public const double XpRevive = 10;
    public const double FallXp = 0.1;
    public const double RechargeCap = 0.25;
    public const double PlunderCap = 0.5;
    public const double GearXpCap = 0.5;

    public const double ShopKills = 30;
    public const double UpgradeShare = 0.5;
    public const double RefundShare = 0.25;
    public const double UpgradeGrowth = 1.25;
    public const double UpgradeBonus = 0.2;
    public const int MaxUpgrades = 100;

    public static readonly double[] RarityPrices = [1, 2, 3, 4];

    public static readonly int[] Tiers = [10, 25, 50, 100, 250];

    public static readonly GrowthSegment[] HpCurve = [new(10, 1.35), new(50, 1.35), new(100, 1.19), new(int.MaxValue, 1.13)];
}
