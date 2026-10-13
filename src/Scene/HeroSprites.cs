using System.Collections.Concurrent;
using System.Collections.Generic;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class HeroSprites
{
    public static readonly HeroLook Default = new("Héros", 0, 0, 1, 0);

    public static readonly Rgba[] Skins = [Palette.Skin, Palette.Peach, Palette.Tan, Palette.Bark];

    public static readonly Rgba[] HairColors = [Palette.Soil, Palette.Bark, Palette.Rust, Palette.Gold, Palette.Silver, Palette.Pink];

    public static readonly (Rgba Main, Rgba Shade)[] Outfits =
    [
        (Palette.Red, Palette.Wine), (Palette.Blue, Palette.NavyBlue), (Palette.Green, Palette.Leaf), (Palette.Pink, Palette.Plum),
        (Palette.Orange, Palette.Brick), (Palette.Gold, Palette.Rust), (Palette.Cyan, Palette.Blue), (Palette.Slate, Palette.Navy),
    ];

    private static readonly ConcurrentDictionary<(HeroRole Role, HeroLook Look), HeroFigure> Cache = new();

    public static HeroFigure Of(HeroRole role, HeroLook? look) =>
        Cache.GetOrAdd((role, Shape(look ?? Default)), key => Compose(key.Role, key.Look));

    private static HeroLook Shape(HeroLook look) => look with { Name = string.Empty };

    private static HeroFigure Compose(HeroRole role, HeroLook look)
    {
        (Rgba main, Rgba shade) = Outfits[look.Outfit];
        Dictionary<char, Rgba> colors = new()
        {
            ['S'] = Skins[look.Skin],
            ['H'] = HairColors[look.HairColor],
            ['O'] = main,
            ['Q'] = shade,
        };
        ArtSprite sprite = ArtSprite.Parse(HeroGrids.Bodies[(int)role], colors).Over(ArtSprite.Parse(HeroGrids.Hairs[look.Hair], colors));
        if (role == HeroRole.Mage)
        {
            sprite = sprite.Over(ArtSprite.Parse(HeroGrids.MageHat, colors));
        }

        return new HeroFigure(sprite, sprite.Silhouette(Palette.Red), sprite.Silhouette(Palette.White), sprite.Lying().Faded(Palette.Gray, 0.45f), sprite.FirstRow);
    }
}
