using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record AnswerArguments([property: JsonPropertyName("p_choice_id")] string ChoiceId);
