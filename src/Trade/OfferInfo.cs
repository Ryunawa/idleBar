using System;

namespace IdleBar.Trade;

public sealed record OfferInfo(
    long Id,
    string TownId,
    string? GiveGoodId,
    int GiveQuantity,
    string? WantGoodId,
    int WantQuantity,
    OfferStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? ClosedAt,
    string? Seller,
    string? Buyer,
    bool Seen);
