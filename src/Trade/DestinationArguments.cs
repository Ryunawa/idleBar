using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record DestinationArguments([property: JsonPropertyName("p_destination_id")] string DestinationId);
