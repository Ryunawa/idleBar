using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Trade;

public sealed record ReputationState(IReadOnlyList<ReputationTier> Tiers, IReadOnlyList<TownReputation> Towns)
{
    public static ReputationState None { get; } = new([], []);

    public int PointsAt(string townId) => Towns.FirstOrDefault(town => town.TownId == townId)?.Points ?? 0;

    public ReputationTier? TierAt(string townId)
    {
        int points = PointsAt(townId);
        return Tiers.Where(tier => tier.Points <= points).MaxBy(tier => tier.Level);
    }

    public ReputationTier? NextTierAt(string townId)
    {
        int points = PointsAt(townId);
        return Tiers.Where(tier => tier.Points > points).MinBy(tier => tier.Level);
    }

    public int StorageBonusAt(string townId) => TierAt(townId)?.StorageBonus ?? 0;

    public double PriceRateAt(string townId) => TierAt(townId)?.PriceRate ?? 0;

    public double FreightDiscountAt(string townId) => TierAt(townId)?.FreightDiscount ?? 0;
}
