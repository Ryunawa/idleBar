namespace IdleBar.Inn;

public static class StationFactory
{
    public static Station For(Drink drink) => drink switch
    {
        Drink.Tea => new TeapotStation(),
        Drink.Soup => new SoupStation(),
        Drink.Cider => new CiderStation(),
        Drink.Pie => new PieStation(),
        _ => new TapStation(),
    };
}
