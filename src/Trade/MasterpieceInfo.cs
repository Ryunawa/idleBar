using System;

namespace IdleBar.Trade;

public sealed record MasterpieceInfo(long Id, string GoodId, string Maker, string TownId, DateTimeOffset CreatedAt, int Price);
