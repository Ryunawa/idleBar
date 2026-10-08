namespace IdleBar.Pixel;

public static class KitchenSprites
{
    public static PixelSprite Cauldron { get; } = PixelSprite.Parse(
        ".NnnnnnnN.",
        "NSaIaIaISN",
        ".SSSSSSSS.",
        "SsSSSSSSSS",
        "SsSSSSSSSS",
        ".SSSSSSSS.",
        "..S....S..");

    public static PixelSprite CauldronBubbling { get; } = PixelSprite.Parse(
        ".NnnnnnnN.",
        "NSIaIaIaSN",
        ".SSSSSSSS.",
        "SsSSSSSSSS",
        "SsSSSSSSSS",
        ".SSSSSSSS.",
        "..S....S..");

    public static PixelSprite Oven { get; } = PixelSprite.Parse(
        "....ssss....",
        "..ssSsssss..",
        ".ssssssSsss.",
        "sSsXXXXXXsSs",
        "ssXXXXXXXXss",
        "sSXXXXXXXXSs",
        "ssXXXXXXXXss",
        "ssssssssssss",
        "SSSSSSSSSSSS");

    public static PixelSprite SoupIcon { get; } = PixelSprite.Parse(
        ".s.s.",
        ".....",
        "aaaaa",
        "naaan",
        ".nnn.");

    public static PixelSprite CiderIcon { get; } = PixelSprite.Parse(
        ".fff.",
        "nLLLn",
        "nLLLn",
        "nLLLn",
        ".nnn.");

    public static PixelSprite PieIcon { get; } = PixelSprite.Parse(
        ".....",
        ".jjj.",
        "jojoj",
        "jjjjj",
        "BBBBB");

    public static PixelSprite SoupBowl { get; } = PixelSprite.Parse(
        "naaan",
        ".nnn.");

    public static PixelSprite CiderGlass { get; } = PixelSprite.Parse(
        "n...n",
        "nLLLn",
        "nLLLn",
        "nLLLn",
        ".nnn.");

    public static PixelSprite PieDish { get; } = PixelSprite.Parse(
        ".jjj.",
        "jojoj",
        "BBBBB");
}
