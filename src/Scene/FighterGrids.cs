using System.Collections.Generic;

namespace IdleBar.Scene;

public static class FighterGrids
{
    public static IReadOnlyList<string> Archer { get; } =
    [
        "...0000.....", "..033330.0..", ".0333333040.", ".033ff0f0040", ".03ffff0..40", "..00ff00..40", ".0bbbbbb0.40",
        "0fb3bb3bff40", "0f0bbbbb0.40", ".00444400.40", "..0bbbb0.040", "..0b00b0.00.", "..0b00b0....", ".000..000...",
    ];

    public static IReadOnlyList<string> Mage { get; } =
    [
        "....00......", "...0220..0..", "...022e0.0c0", "..022a220.0.", ".0000000004.", "..0ffff0..4.", "..0ff0f0..4.",
        "..00ff00..4.", ".0e2222e0.4.", "0f2222222ff.", "0f0222220.4.", ".02e22e20.4.", ".0222222204.", ".022e2e2204.", ".0000000000.",
    ];

    public static IReadOnlyList<string> ChickenUp { get; } =
    [
        ".8.......", "88...99..", "9aa.9aa9.", "a0aaaa9..", ".aaaaaaa9", "..aaaaaa9", "...9aaa9.", "....9.9..",
    ];

    public static IReadOnlyList<string> ChickenDown { get; } =
    [
        ".8.......", "88.......", "9aa......", "a0aaaaa9.", ".aa999aa9", "..a9aa9a9", "...99aa9.", "....9.9..",
    ];

    public static IReadOnlyList<string> Lock { get; } = [".666.", "6...6", "99999", "99099", "99099", "99999"];

    public static IReadOnlyList<string> Bread { get; } = [".fff.", "f9f9f", ".fff."];
}
