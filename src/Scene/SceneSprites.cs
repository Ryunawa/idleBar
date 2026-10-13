using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Scene;

public static class SceneSprites
{
    public static ArtSprite Archer { get; } = ArtSprite.Parse(FighterGrids.Archer);

    public static ArtSprite ArcherFlash { get; } = Archer.Silhouette(Palette.White);

    public static ArtSprite ArcherDown { get; } = Archer.Lying().Faded(Palette.Gray, 0.45f);

    public static ArtSprite Mage { get; } = ArtSprite.Parse(FighterGrids.Mage);

    public static ArtSprite MageFlash { get; } = Mage.Silhouette(Palette.White);

    public static ArtSprite MageDown { get; } = Mage.Lying().Faded(Palette.Gray, 0.45f);

    public static IReadOnlyList<ArtSprite> Chicken { get; } = [ArtSprite.Parse(FighterGrids.ChickenUp), ArtSprite.Parse(FighterGrids.ChickenDown)];

    public static ArtSprite Lock { get; } = ArtSprite.Parse(FighterGrids.Lock);

    public static ArtSprite Bread { get; } = ArtSprite.Parse(FighterGrids.Bread);

    public static IReadOnlyList<ArtSprite> Skills { get; } =
        [ArtSprite.Parse(FighterGrids.Bolt), ArtSprite.Parse(FighterGrids.Flame), ArtSprite.Parse(FighterGrids.Coins)];

    public static IReadOnlyList<ArtSprite?> Monsters { get; } =
        [.. MonsterGrids.Templates.Select(grid => grid is null ? null : ArtSprite.Parse(grid))];

    public static IReadOnlyList<ArtSprite?> WhiteMonsters { get; } = [.. Monsters.Select(sprite => sprite?.Silhouette(Palette.White))];

    public static IReadOnlyList<ArtSprite> Bosses { get; } = [.. MonsterGrids.Bosses.Select(grid => ArtSprite.Parse(grid))];

    public static IReadOnlyList<ArtSprite> WhiteBosses { get; } = [.. Bosses.Select(sprite => sprite.Silhouette(Palette.White))];

    public static IReadOnlyList<ArtSprite> Shadows { get; } =
        [.. Monsters.OfType<ArtSprite>().Select(sprite => sprite.Silhouette(Palette.Ink))];
}
