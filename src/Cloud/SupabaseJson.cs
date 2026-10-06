using System.Text.Json;

namespace IdleBar.Cloud;

internal static class SupabaseJson
{
    public static JsonSerializerOptions Options { get; } = new() { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
}
