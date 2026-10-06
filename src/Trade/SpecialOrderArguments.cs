using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record SpecialOrderArguments([property: JsonPropertyName("p_order_id")] long OrderId);
