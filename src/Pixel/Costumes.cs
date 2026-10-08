namespace IdleBar.Pixel;

public static class Costumes
{
    public static PatronLook? For(int seed, Festival festival)
    {
        if (festival == Festival.None || seed % 3 != 0)
        {
            return null;
        }

        int head = festival == Festival.Christmas ? PatronSprites.Cap : seed % 2 == 0 ? PatronSprites.PumpkinHead : PatronSprites.Witch;
        return PatronLook.From(seed) with { Head = head };
    }
}
