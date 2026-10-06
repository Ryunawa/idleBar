namespace IdleBar.Trade;

public sealed record ExchangeNews(int Concluded, int Expired, int Delivered, int Failed)
{
    public static ExchangeNews None { get; } = new(0, 0, 0, 0);

    public int Total => Concluded + Expired + Delivered + Failed;

    public int Offers => Concluded + Expired;

    public int Contracts => Delivered + Failed;
}
