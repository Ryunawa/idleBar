using System.Collections.Generic;

namespace IdleBar.Trade;

public sealed record TownInfo(
    string Id,
    string Name,
    Biome Biome,
    int MapX,
    int MapY,
    IReadOnlyList<string> Produces,
    IReadOnlyList<string> Demands);
