using System.Collections.Concurrent;
using System.Collections.Generic;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class HeroSprites
{
    public static readonly HeroLook Default = new("Héros", 0, 0, 1, 0);

    public static readonly Rgba[] Skins = [Palette.LightPeach, Palette.PicoOrange, Palette.Brown, Palette.DarkGrey];

    public static readonly Rgba[] HairColors =
        [Palette.Black, Palette.Brown, Palette.PicoOrange, Palette.PicoYellow, Palette.LightGrey, Palette.PicoPink];

    public static readonly (Rgba Main, Rgba Shade)[] Outfits =
    [
        (Palette.PicoRed, Palette.DarkPurple), (Palette.PicoBlue, Palette.DarkBlue), (Palette.PicoGreen, Palette.DarkGreen),
        (Palette.PicoPink, Palette.DarkPurple), (Palette.PicoOrange, Palette.Brown), (Palette.PicoYellow, Palette.PicoOrange),
        (Palette.Lavender, Palette.DarkBlue), (Palette.DarkGrey, Palette.Black),
    ];

    private static readonly (Rgba Main, Rgba Shade)[] Weapons =
    [
        (Palette.LightGrey, Palette.DarkGrey), (Palette.PicoBlue, Palette.DarkBlue), (Palette.Brown, Palette.DarkPurple),
        (Palette.PicoBlue, Palette.DarkBlue), (Palette.PicoYellow, Palette.PicoOrange),
    ];

    private static readonly ConcurrentDictionary<(HeroRole Role, HeroLook Look, GearLook Worn), HeroFigure> Cache = new();

    public static HeroFigure Of(HeroRole role, HeroLook? look, GearLook? worn = null) =>
        Cache.GetOrAdd((role, Shape(look ?? Default), worn ?? GearLook.None), key => Compose(key.Role, key.Look, key.Worn));

    private static HeroLook Shape(HeroLook look) => look with { Name = string.Empty };

    private static HeroFigure Compose(HeroRole role, HeroLook look, GearLook worn)
    {
        (Rgba main, Rgba shade) = Outfits[look.Outfit];
        (Rgba blade, Rgba edge) = worn.Weapon is int weapon ? ItemSprites.Rarities[weapon] : Weapons[(int)role];
        (Rgba plate, Rgba seam) = worn.Armor is int armor ? ItemSprites.Rarities[armor] : (Palette.LightGrey, Palette.DarkGrey);
        Dictionary<char, Rgba> colors = new()
        {
            ['S'] = Skins[look.Skin],
            ['H'] = HairColors[look.HairColor],
            ['O'] = main,
            ['Q'] = shade,
            ['W'] = blade,
            ['V'] = edge,
            ['X'] = plate,
            ['Y'] = seam,
            ['G'] = worn.Arms is int arms ? ItemSprites.Rarities[arms].Main : Skins[look.Skin],
            ['L'] = worn.Legs is int legs ? ItemSprites.Rarities[legs].Main : shade,
            ['F'] = worn.Feet is int feet ? ItemSprites.Rarities[feet].Shade : shade,
        };
        ArtSprite sprite = ArtSprite.Parse(HeroGrids.Bodies[(int)role], colors).Over(ArtSprite.Parse(HeroGrids.Hairs[look.Hair], colors));
        if (worn.Helm is int helm)
        {
            sprite = sprite.Over(ItemSprites.Helm(helm, worn.HelmRarity));
        }
        else if (role == HeroRole.Mage)
        {
            sprite = sprite.Over(ArtSprite.Parse(HeroGrids.MageHat, colors));
        }

        return new HeroFigure(sprite, sprite.Silhouette(Palette.PicoRed), sprite.Silhouette(Palette.PicoYellow), sprite.Silhouette(Palette.PicoWhite), sprite.Lying().Faded(Palette.Lavender, 0.5f), sprite.FirstRow);
    }
}
