namespace IdleBar.Pixel;

public static class FolkSprites
{
    private static readonly string[] Standing = ["..k.k.", "..k.k."];
    private static readonly string[] Striding = [".k..k.", "k....k"];
    private static readonly string[] Passing = ["..kk..", "..kk.."];

    public static Walker Baker { get; } = Create("..BB..", "..pp..", ".rrrr.", ".rrrrp", ".rrrr.", ".rrrr.");

    public static Walker Farmer { get; } = Create("..kk..", "..pp..", ".VVVV.", ".VVVVp", ".VVVV.", ".VVVV.");

    public static Walker Pilgrim { get; } = Create("..OO.b", "..pp.b", ".OOOOb", ".OOOOp", ".OOOOb", ".OOOOb");

    public static Walker Peddler { get; } = Create("..kk..", "..pp..", "bbaaa.", "bbaaap", "bbaaa.", ".aaaa.");

    public static Walker Porter { get; } = Create("..pp...", "..pp...", ".qqqqbb", ".qqqqBB", ".qqqq..", ".qqqq..");

    public static Walker Courier { get; } = Create("..pp..", "..pp..", ".qqqq.", ".qqqqp", ".qqqq.", ".qqqq.");

    public static PixelSprite BirdUp { get; } = PixelSprite.Parse("x...x", ".xxx.");

    public static PixelSprite BirdDown { get; } = PixelSprite.Parse(".xxx.", "x...x");

    private static Walker Create(params string[] body) =>
        new(PixelSprite.Parse([.. body, .. Standing]), PixelSprite.Parse([.. body, .. Striding]), PixelSprite.Parse([.. body, .. Passing]));
}