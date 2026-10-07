namespace IdleBar.Trade;

public sealed record OfferDraft(string? GiveGoodId, int GiveQuantity, string? WantGoodId, int WantQuantity, bool FromWarehouse);
