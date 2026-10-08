using System.Collections.Generic;
using System.Linq;
using IdleBar.Inn;
using IdleBar.Online;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public static class TavernSetup
{
    public static void Configure(Tavern tavern, int stools, IReadOnlyList<string> menu, int helper)
    {
        List<Drink> drinks = menu.Select(DrinkMenu.FromId).OfType<Drink>().ToList();
        tavern.Configure(stools, drinks.Count > 0 ? drinks : DrinkMenu.Starters, helper);
    }

    public static DecorSet Decor(WorldData? world, IReadOnlyList<string> upgrades) =>
        DecorSet.From(world?.Upgrades.Where(upgrade => upgrade.Kind == "decor" && upgrades.Contains(upgrade.Id)).Select(upgrade => upgrade.Value) ?? []);
}
