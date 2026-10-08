using System.Text.Json.Nodes;

namespace IdleBar.Online;

public static class RealtimeMessages
{
    public static (RealtimeSignal Signal, RealtimeEvent? Event) Read(string text, string topic)
    {
        JsonNode? node = JsonNode.Parse(text);
        if (node?["topic"]?.GetValue<string>() != topic)
        {
            return (RealtimeSignal.Ignored, null);
        }

        string? status = node["payload"]?["status"]?.GetValue<string>();
        return node["event"]?.GetValue<string>() switch
        {
            "phx_reply" when status == "ok" => (RealtimeSignal.Joined, null),
            "phx_reply" or "phx_error" or "phx_close" => (RealtimeSignal.Dropped, null),
            "broadcast" when node["payload"] is JsonObject payload => (RealtimeSignal.Received, new RealtimeEvent(
                payload["event"]?.GetValue<string>() ?? string.Empty,
                payload["payload"]?.ToJsonString() ?? "{}")),
            _ => (RealtimeSignal.Ignored, null),
        };
    }

    public static string Join(string topic, string token, string reference) => new JsonObject
    {
        ["topic"] = topic,
        ["event"] = "phx_join",
        ["payload"] = new JsonObject
        {
            ["config"] = new JsonObject
            {
                ["broadcast"] = new JsonObject { ["ack"] = false, ["self"] = false },
                ["presence"] = new JsonObject { ["key"] = string.Empty },
                ["private"] = true,
            },
            ["access_token"] = token,
        },
        ["ref"] = reference,
        ["join_ref"] = reference,
    }.ToJsonString();

    public static string AccessToken(string topic, string token, string reference, string joinReference) => new JsonObject
    {
        ["topic"] = topic,
        ["event"] = "access_token",
        ["payload"] = new JsonObject { ["access_token"] = token },
        ["ref"] = reference,
        ["join_ref"] = joinReference,
    }.ToJsonString();

    public static string Heartbeat(string reference) => new JsonObject
    {
        ["topic"] = "phoenix",
        ["event"] = "heartbeat",
        ["payload"] = new JsonObject(),
        ["ref"] = reference,
    }.ToJsonString();
}
