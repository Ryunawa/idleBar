namespace IdleBar.Pixel;

public static class GoodIcons
{
    public const int TallestIcon = 5;

    private static readonly PixelSprite Tools = PixelSprite.Parse("sss", ".b.", ".b.", ".b.");
    private static readonly PixelSprite Horseshoes = PixelSprite.Parse("s...s", "s...s", ".sss.");
    private static readonly PixelSprite Crates = PixelSprite.Parse("bBbB", "BbBb", "bBbB");
    private static readonly PixelSprite Wheels = PixelSprite.Parse(".bbb.", "b.b.b", "bbsbb", "b.b.b", ".bbb.");
    private static readonly PixelSprite Cloth = PixelSprite.Parse(".qqq.", "qqqqq", ".qqq.");
    private static readonly PixelSprite Tarps = PixelSprite.Parse("wWwWw", "WwWwW", "wWwWw");
    private static readonly PixelSprite Remedies = PixelSprite.Parse(".s.", "lll", "lVl", "lll");
    private static readonly PixelSprite Ointment = PixelSprite.Parse("kkkk", "yLLy", "yyyy");
    private static readonly PixelSprite Wagon = PixelSprite.Parse(".www.", "bbbbb", ".k.k.");
    private static readonly PixelSprite Masterpiece = PixelSprite.Parse("..g..", ".gLg.", "gLLLg", ".gLg.", "..g..");

    public static PixelSprite For(string goodId) => goodId switch
    {
        "outils" => Tools,
        "ferrures" => Horseshoes,
        "roues" => Wheels,
        "drap" => Cloth,
        "baches" => Tarps,
        "remedes" => Remedies,
        "onguent" => Ointment,
        "chariot" => Wagon,
        "chef_oeuvre" => Masterpiece,
        _ => Crates,
    };
}
