using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record TalentArguments([property: JsonPropertyName("p_talent_id")] string TalentId);
