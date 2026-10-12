namespace IdleBar.Scene;

public static class Noise
{
    public static int Hash(int value)
    {
        uint mixed = unchecked((uint)value);
        mixed ^= mixed >> 16;
        mixed = unchecked(mixed * 0x7feb352d);
        mixed ^= mixed >> 15;
        mixed = unchecked(mixed * 0x846ca68b);
        mixed ^= mixed >> 16;
        return (int)(mixed & 0x7fffffff);
    }

    public static float Unit(int value, int salt) => Hash(value * 7919 + salt * 104729) % 10_000 / 10_000f;
}
