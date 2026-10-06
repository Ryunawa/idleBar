using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record MasterpieceArguments([property: JsonPropertyName("p_masterpiece_id")] long MasterpieceId);
