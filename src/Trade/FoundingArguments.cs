using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record FoundingArguments(
    [property: JsonPropertyName("p_name")] string Name,
    [property: JsonPropertyName("p_craft_id")] string CraftId,
    [property: JsonPropertyName("p_town_id")] string TownId);
