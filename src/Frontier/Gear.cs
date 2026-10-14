using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Frontier;

public sealed record Gear(double Damage, double Health, double Critical, double Healing, double Speed)
{
    public static Gear None { get; } = new(0, 0, 0, 0, 0);

    public static Gear Of(IEnumerable<(string Stat, double Percent)> stats)
    {
        List<(string Stat, double Percent)> all = [.. stats];
        double Sum(string stat) => all.Where(entry => entry.Stat == stat).Sum(entry => entry.Percent) / 100;
        return new Gear(Sum(Items.Damage), Sum(Items.Health), Sum(Items.Critical), Sum(Items.Healing), Sum(Items.Speed));
    }
}
