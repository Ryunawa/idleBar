using System.Collections.Generic;

namespace IdleBar.Trade;

public sealed record MasteryState(
    string CraftId,
    int Xp,
    MasteryRank Rank,
    int? NextRankXp,
    IReadOnlyList<string> Talents,
    IReadOnlyDictionary<string, double> Bonuses)
{
    public double Bonus(string effect) => Bonuses.TryGetValue(effect, out double amount) ? amount : 0;
}
