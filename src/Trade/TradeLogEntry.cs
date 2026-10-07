using System;

namespace IdleBar.Trade;

public sealed record TradeLogEntry(
    long Id,
    string TownId,
    string GoodId,
    string Side,
    string Source,
    int Quantity,
    long Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public bool Bought => Side == "buy";

    public double UnitPrice => Quantity > 0 ? (double)Total / Quantity : 0;
}