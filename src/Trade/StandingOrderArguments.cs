using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record StandingOrderArguments(
    [property: JsonPropertyName("p_kind_id")] string KindId,
    [property: JsonPropertyName("p_choice_id")] string ChoiceId);
