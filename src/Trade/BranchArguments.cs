using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record BranchArguments([property: JsonPropertyName("p_town_id")] string TownId);
