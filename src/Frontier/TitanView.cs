namespace IdleBar.Frontier;

public sealed record TitanView(string Week, int Kind, long Hp, long Damage, bool Fallen, long Mine, bool Rewarded = false)
{
    public double Left => Hp <= 0 ? 0 : System.Math.Clamp(1 - (double)Damage / Hp, 0, 1);
}
