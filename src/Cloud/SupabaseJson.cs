using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdleBar.Cloud;

internal static class SupabaseJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
    };
}
