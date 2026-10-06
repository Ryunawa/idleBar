namespace IdleBar.Pixel;

public sealed record CaravanLook(bool Tarp, bool IronWheels)
{
    public static CaravanLook Plain { get; } = new(false, false);
}
