namespace IdleBar.Pixel;

public sealed record CaravanLook(bool Tarp, bool IronWheels, int Wagons = 1)
{
    public static CaravanLook Plain { get; } = new(false, false);
}