using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class TeamReactions
{
    private static readonly Rgba[] Blood = [Palette.Red, Palette.Wine];
    private static readonly Rgba[] Dust = [Palette.Sand, Palette.Gray, Palette.Wood];
    private static readonly Rgba[] Glitter = [Palette.Yellow, Palette.White, Palette.Cyan];
    private static readonly Rgba[] Mend = [Palette.Green, Palette.White];

    public static void Apply(FrontierScene scene, FrontierEvent frontierEvent, FrontierState state, FrontierLayout layout, MonsterBox target, Random random)
    {
        SceneFx fx = scene.Fx;
        SceneMotion motion = scene.Motion;
        switch (frontierEvent)
        {
            case Struck struck:
                motion.MonsterLunge = 0.12f;
                motion.Hurt[(int)struck.Target] = 0.1f;
                fx.Burst(Center(layout, struck.Target, motion), 17, struck.Boss ? 5 : 3, Blood, random);
                break;
            case Fell fell:
                fx.Burst(Center(layout, fell.Kind, motion), 21, 8, Dust, random);
                break;
            case Revived revived:
                fx.Burst(Center(layout, revived.Kind, motion), 16, 8, Glitter, random);
                break;
            case Routed:
                motion.Fleeing = target;
                motion.FleeTime = 0;
                fx.Float("L'ÉQUIPE RECULE", layout.HeroX + 6, 1, Palette.Red, 2.2f, 0);
                break;
            case Healed:
                foreach (FighterInfo fighter in Fighters.All)
                {
                    if (Vitality.Up(state, fighter.Kind))
                    {
                        fx.Burst(Center(layout, fighter.Kind, motion), 15, 2, Mend, random);
                    }
                }

                break;
            case Armored:
                fx.Float("+1", layout.HeroX + 4, 7, Palette.Yellow, 0.7f);
                break;
        }
    }

    private static int Center(FrontierLayout layout, FighterKind kind, SceneMotion motion) => FrontPainter.Walk(layout, kind, motion) + 5;
}
