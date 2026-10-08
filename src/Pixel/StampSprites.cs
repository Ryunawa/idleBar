using System.Collections.Generic;

namespace IdleBar.Pixel;

public static class StampSprites
{
    private static readonly Dictionary<string, PixelSprite> Sprites = new()
    {
        ["heart"] = PixelSprite.Parse(".rr.rr.", "rrrrrrr", "rrrrrrr", ".rrrrr.", "..rrr..", "...r..."),
        ["star"] = PixelSprite.Parse("...g...", "..ggg..", "ggggggg", ".ggggg.", ".gg.gg.", "g.....g"),
        ["mug"] = PixelSprite.Parse("fffff..", "eeeeenn", "eeeee.n", "eeeeenn", "eeeee..", "NNNNN.."),
        ["sun"] = PixelSprite.Parse("g..g..g", ".ggggg.", "gggLggg", ".ggggg.", "g..g..g"),
        ["moon"] = PixelSprite.Parse("..LLL..", ".LL....", "LL.....", "LL.....", ".LL....", "..LLL.."),
        ["flower"] = PixelSprite.Parse("..r.r..", ".rrgrr.", "..rrr..", "...V...", ".VVV...", "...V..."),
        ["note"] = PixelSprite.Parse("..hhhh.", "..h..h.", "..h..h.", ".hh.hh.", "hhhhhh.", ".hh.hh."),
        ["crown"] = PixelSprite.Parse("g..g..g", "gg.g.gg", "ggggggg", "grgqgrg", "ggggggg"),
    };

    public static PixelSprite? For(string stamp) => Sprites.GetValueOrDefault(stamp);
}
