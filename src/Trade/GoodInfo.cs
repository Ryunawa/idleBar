namespace IdleBar.Trade;

public sealed record GoodInfo(string Id, string Name, string? OneName, string? ManyName, int BasePrice, bool Crafted, bool MarketSells);
