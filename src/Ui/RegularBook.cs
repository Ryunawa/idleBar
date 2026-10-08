using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Inn;
using IdleBar.Online;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public sealed class RegularBook : IRegularBook
{
    private const double VisitChance = 0.25;
    private const float Dusk = 0.5f;

    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(10);

    private readonly Dictionary<string, DateTime> _lastArrival = [];
    private WorldData? _world;
    private TavernData? _tavern;
    private HashSet<string> _decor = [];

    public void Update(WorldData? world, TavernData? tavern)
    {
        _world = world;
        _tavern = tavern;
        _decor = world is null || tavern is null
            ? []
            : world.Upgrades.Where(upgrade => upgrade.Kind == "decor" && tavern.Upgrades.Contains(upgrade.Id)).Select(upgrade => upgrade.Value).ToHashSet();
    }

    public RegularInfo? Find(string? regular) => regular is null ? null : _world?.Regulars.FirstOrDefault(each => each.Id == regular);

    public string? Pick(IReadOnlyCollection<string> present, Random random)
    {
        if (_world is null || _tavern is null || random.NextDouble() >= VisitChance)
        {
            return null;
        }

        DateTime now = DateTime.Now;
        List<RegularInfo> eligible = _world.Regulars
            .Where(regular => !present.Contains(regular.Id) && Rested(regular.Id, now) && Welcome(regular.Condition, now))
            .ToList();
        if (eligible.Count == 0)
        {
            return null;
        }

        RegularInfo chosen = eligible[random.Next(eligible.Count)];
        _lastArrival[chosen.Id] = now;
        return chosen.Id;
    }

    public Drink? Favorite(string regular) => Find(regular) is RegularInfo info ? DrinkMenu.FromId(info.Drink) : null;

    private bool Rested(string regular, DateTime now) => !_lastArrival.TryGetValue(regular, out DateTime last) || now - last > Cooldown;

    private bool Welcome(string condition, DateTime now)
    {
        bool night = SkyLight.At(now).Daylight < Dusk;
        string[] parts = condition.Split(':');
        return parts[0] switch
        {
            "any" => true,
            "day" => !night,
            "night" => night,
            "rain" => Weather.Raining(now),
            "clear-night" => night && !Weather.Raining(now),
            "menu" when parts.Length == 2 => _tavern!.Menu.Contains(parts[1]),
            "decor" when parts.Length == 2 => _decor.Contains(parts[1]),
            "tier" when parts.Length == 2 && int.TryParse(parts[1], out int tier) => _tavern!.Tier >= tier,
            _ => false,
        };
    }
}
