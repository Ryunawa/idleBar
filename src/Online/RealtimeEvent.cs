namespace IdleBar.Online;

public sealed record RealtimeEvent(string Name, string Payload)
{
    public const string Lost = "lost";
}
