using System;

namespace IdleBar.Trade;

public sealed record SpecialOrderInfo(
    long Id,
    string TownId,
    string GoodId,
    int Quantity,
    int UnitPrice,
    DateTimeOffset CreatedAt,
    DateTimeOffset Deadline,
    SpecialOrderStatus Status,
    DateTimeOffset? ClosedAt,
    bool Crafted = false,
    int Produced = 0)
{
    public bool IsReady(int held) => held >= Quantity && (!Crafted || Produced >= Quantity);
}
