namespace IdleBar.Inn;

public static class DrinkMenu
{
    public const int PerfectTip = 3;
    public const int QuickTip = 1;
    public const int Parting = 1;

    public static int Price(Drink drink) => drink switch
    {
        Drink.Beer => 4,
        Drink.Tea => 6,
        _ => 0,
    };

    public static string Name(Drink drink) => drink switch
    {
        Drink.Beer => "Bière",
        Drink.Tea => "Thé",
        _ => string.Empty,
    };
}
