using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record TradeArguments(
    [property: JsonPropertyName("p_good_id")] string GoodId,
    [property: JsonPropertyName("p_quantity")] int Quantity);
