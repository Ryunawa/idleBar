namespace IdleBar.Pixel;

public static class DecorSizes
{
    public const int Shelf = 30;
    public const int Fireplace = 24;
    public const int Barrels = 15;

    public static int Width(DecorKind kind) => kind switch
    {
        DecorKind.Window => WindowPainter.Width,
        DecorKind.Shelf => Shelf,
        DecorKind.Fireplace => Fireplace,
        DecorKind.Barrels or DecorKind.SleepingCat => Barrels,
        _ => DecorSprites.For(kind).Width,
    };
}
