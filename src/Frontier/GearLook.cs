namespace IdleBar.Frontier;

public sealed record GearLook(int? Weapon, int? Armor, int? Helm, int HelmRarity)
{
    public static GearLook None { get; } = new(null, null, null, 0);
}
