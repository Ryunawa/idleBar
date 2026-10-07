namespace IdleBar.Trade;

public sealed record MarketQuote(string TownId, string GoodId, double BuyPrice, double SellPrice, string? Trend = null)
{
    public bool Cheap => Trend == "cheap";

    public bool Dear => Trend == "dear";
}
