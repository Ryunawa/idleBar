using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record OfferArguments(
    [property: JsonPropertyName("p_give_good_id")] string? GiveGoodId,
    [property: JsonPropertyName("p_give_quantity")] int GiveQuantity,
    [property: JsonPropertyName("p_want_good_id")] string? WantGoodId,
    [property: JsonPropertyName("p_want_quantity")] int WantQuantity,
    [property: JsonPropertyName("p_town_id")] string TownId);
