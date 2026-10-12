using System;

namespace IdleBar.Online;

public sealed record FrontierReply(string Status, DateTimeOffset ServerTime, FrontierData? Frontier)
{
    public const string Ok = "ok";
    public const string Elsewhere = "elsewhere";
    public const string Conflict = "conflict";
    public const string Refused = "refused";
    public const string Missing = "missing";
}
