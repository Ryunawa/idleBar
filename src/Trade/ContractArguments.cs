using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record ContractArguments(
    [property: JsonPropertyName("p_good_id")] string GoodId,
    [property: JsonPropertyName("p_quantity")] int Quantity,
    [property: JsonPropertyName("p_destination_id")] string DestinationId,
    [property: JsonPropertyName("p_reward")] int Reward,
    [property: JsonPropertyName("p_town_id")] string TownId);
