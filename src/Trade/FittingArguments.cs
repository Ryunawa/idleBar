using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record FittingArguments([property: JsonPropertyName("p_fitting_id")] string FittingId);
