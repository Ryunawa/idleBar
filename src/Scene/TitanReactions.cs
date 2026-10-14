using System;
using IdleBar.Frontier;
using IdleBar.Ui;

namespace IdleBar.Scene;

public static class TitanReactions
{
    private static readonly Rgba[] Sparks = [Palette.Gold, Palette.Yellow, Palette.White];
    private static readonly Rgba[] Rubble = [Palette.Gray, Palette.Steel, Palette.Sand];

    public static void Apply(FrontierScene scene, FrontierEvent frontierEvent, FrontierState state, FrontierLayout layout, Random random)
    {
        SceneMotion motion = scene.Motion;
        ArtPoint? place = TitanPainter.Place(state, layout);
        int center = place is ArtPoint point ? point.X + 16 : layout.FogX + layout.FogWidth / 2;
        switch (frontierEvent)
        {
            case TitanAppeared { Fallen: false } appeared:
                scene.Banners.Add(["TITAN DE LA SEMAINE", $"{Titans.Name(appeared.Kind)} · {Math.Ceiling(appeared.Left * 100)} % DE PV"], 4.5f);
                motion.Shake = 0.4f;
                break;
            case TitanWounded wounded:
                scene.Banners.Add(["LE TITAN VACILLE", $"PLUS QUE {Math.Ceiling(wounded.Left * 100)} % DE PV"], 4);
                motion.TitanFlash = 0.4f;
                break;
            case TitanFell fell:
                scene.Banners.Add(["LE TITAN EST TOMBÉ !", fell.Rewarded ? "RÉCLAME TA RÉCOMPENSE" : "VICTOIRE DE TOUTES LES FRONTIÈRES"], 6);
                motion.TitanFall = 0;
                motion.Shake = 1.2f;
                motion.Flash = 0.3f;
                scene.Fx.Burst(center, 20, 30, Rubble, random);
                break;
            case TitanStruck struck when place is not null:
                motion.TitanFlash = 0.25f;
                scene.Fx.Burst(center, 12, 10, Sparks, random);
                scene.Fx.Float($"+{NumberFormat.Amount(struck.Monsters)}", center, 9, Palette.Gold, 1.6f, -6);
                break;
        }
    }
}
