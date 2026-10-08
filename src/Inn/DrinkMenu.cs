using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public static class DrinkMenu
{
    public const int PerfectTip = 3;
    public const int QuickTip = 1;
    public const int Parting = 1;
    private const float QuickService = 20f;

    public static IReadOnlyList<Drink> Starters { get; } = [Drink.Beer, Drink.Tea];

    public static int Price(Drink drink) => drink switch
    {
        Drink.Beer => 4,
        Drink.Tea => 6,
        Drink.Soup => 8,
        Drink.Cider => 10,
        Drink.Pie => 14,
        _ => 0,
    };

    public static int Bill(PreparedDrink drink, float waited) =>
        Price(drink.Drink) + (drink.Perfect ? PerfectTip : 0) + (!drink.Helped && waited < QuickService ? QuickTip : 0);

    public static int Appetite(Drink drink) => drink switch
    {
        Drink.Beer => 35,
        Drink.Tea => 25,
        Drink.Soup => 20,
        Drink.Cider => 12,
        Drink.Pie => 8,
        _ => 0,
    };

    public static string Name(Drink drink) => drink switch
    {
        Drink.Beer => "Bière",
        Drink.Tea => "Thé",
        Drink.Soup => "Soupe",
        Drink.Cider => "Cidre",
        Drink.Pie => "Tourte",
        _ => string.Empty,
    };

    public static string Id(Drink drink) => drink.ToString().ToLowerInvariant();

    public static Drink? FromId(string id) =>
        System.Enum.GetValues<Drink>().Cast<Drink?>().FirstOrDefault(drink => Id(drink!.Value) == id);
}
