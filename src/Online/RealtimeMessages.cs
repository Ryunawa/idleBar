using System.Text.Json.Nodes;

namespace IdleBar.Online;

public static class RealtimeMessages
{
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
