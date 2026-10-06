using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record MarketArguments(
    [property: JsonPropertyName("p_good_id")] string GoodId,
    [property: JsonPropertyName("p_quantity")] int Quantity,
    [property: JsonPropertyName("p_town_id")] string TownId);
