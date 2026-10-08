using System.Collections.Generic;

namespace IdleBar.Online;

public sealed record WorldData(
    IReadOnlyList<TierInfo> Tiers,
    IReadOnlyList<DrinkInfo> Drinks,
    IReadOnlyList<UpgradeInfo> Upgrades,
    IReadOnlyList<RegularInfo> Regulars,
    IReadOnlyList<string> Bases,
    IReadOnlyList<string> Complements,
    IReadOnlyList<StampInfo> Stamps);
