namespace IdleBar.Pixel;

public readonly record struct StreetPlot(StreetBuilding Building, int X, int Width, int Height)
{
    private const int Reach = 2;

    public int Right => X + Width;

    public bool Contains(int x, int y, int ground) =>
        x >= X - 1 && x <= Right && y >= ground - Height - Reach && y <= ground + Reach;
}