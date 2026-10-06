using System.Text.Json.Serialization;

namespace IdleBar.Trade;

internal sealed record ProductionArguments(
    [property: JsonPropertyName("p_recipe_id")] string RecipeId,
    [property: JsonPropertyName("p_batches")] int Batches);
