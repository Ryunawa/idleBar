using System;
using System.Collections.Generic;

namespace IdleBar.Pixel;

public static class StreetSprites
{
    private const int MinBarnWidth = 14;
    private const int BarnStep = 2;
    private const int BarnTiers = 4;

    private static readonly PixelSprite[] Barns = [.. BuildBarns()];

    public static PixelSprite Board { get; } = PixelSprite.Parse(
        "bbbbbbbbbbbb",
        "bOOOOOOOOOOb",
        "bOOOOOOOOOOb",
        "bOOOOOOOOOOb",
        "bOOOOOOOOOOb",
        "bOOOOOOOOOOb",
        "bbbbbbbbbbbb",
        "..b......b..",
        "..b......b..",
        "..b......b..",
        ".BBB....BBB.");

    public static PixelSprite Relay { get; } = PixelSprite.Parse(
        "....SSSSSS....",
        "...SssssssS...",
        "..SssssssssS..",
        ".SSSSSSSSSSSS.",
        "..yYyyyyyyYy..",
        "..yLLyyyyLLy.g",
        "..yLLyyyyLLyg.",
        "..yyyyyyyyyy..",
        "..yyyyqqyyyy..",
        "..yyyyqqyyyy..",
        "..yyyyqqyyyy..");

    public static PixelSprite CrierRinging { get; } = PixelSprite.Parse(
        ".....g",
        "....gp",
        "..pp..",
        "..pp..",
        ".aaaa.",
        ".aaaa.",
        ".aaaa.",
        "..k.k.",
        "bbbbbb",
        "b....b");

    public static PixelSprite CrierWaiting { get; } = PixelSprite.Parse(
        "......",
        "......",
        "..pp..",
        "..pp..",
        ".aaaap",
        ".aaaag",
        ".aaaa.",
        "..k.k.",
        "bbbbbb",
        "b....b");

    public static PixelSprite Signpost { get; } = PixelSprite.Parse(
        "...b.....",
        "...booooo",
        "...boooo.",
        "oooob....",
        ".ooob....",
        "...b.....",
        "...b.....",
        "...b.....",
        "..BBB....");

    public static PixelSprite Crate { get; } = PixelSprite.Parse(
        "bBbB",
        "BobB",
        "bBbB");

    public static PixelSprite Barn(int capacityTier) => Barns[Math.Clamp(capacityTier, 0, BarnTiers - 1)];

    private static IEnumerable<PixelSprite> BuildBarns()
    {
        for (int tier = 0; tier < BarnTiers; tier++)
        {
            yield return PixelSprite.Parse([.. BarnRows(MinBarnWidth + tier * BarnStep, 5 + tier, 3 + tier / 2)]);
        }
    }

    private static IEnumerable<string> BarnRows(int width, int wallRows, int roofRows)
    {
        for (int row = 0; row < roofRows; row++)
        {
            int inset = (roofRows - 1 - row) * 2;
            char[] cells = new string('.', width).ToCharArray();
            for (int x = inset; x < width - inset; x++)
            {
                cells[x] = row == roofRows - 1 || x == inset || x == width - inset - 1 ? 'R' : 'r';
            }

            yield return new string(cells);
        }

        int doorWidth = width / 3;
        int doorLeft = (width - doorWidth) / 2;
        for (int row = 0; row < wallRows; row++)
        {
            char[] cells = new string('.', width).ToCharArray();
            for (int x = 1; x < width - 1; x++)
            {
                bool frame = x == 1 || x == width - 2 || row == wallRows - 1;
                bool door = x >= doorLeft && x < doorLeft + doorWidth && row >= wallRows / 3;
                bool doorFrame = door && (x == doorLeft || x == doorLeft + doorWidth - 1 || row == wallRows / 3);
                cells[x] = frame ? 'b' : doorFrame ? 'B' : door ? 'k' : x % 3 == 0 ? 'B' : 'O';
            }

            yield return new string(cells);
        }
    }
}