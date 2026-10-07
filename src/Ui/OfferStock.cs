namespace IdleBar.Ui;

public sealed record OfferStock(string? GoodId, bool FromWarehouse, string Label, long Available)
{
    public string? PickerId => GoodId is null ? null : FromWarehouse ? $"entrepôt:{GoodId}" : $"cale:{GoodId}";
}
