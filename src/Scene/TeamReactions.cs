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
                Hurt(motion, struck.Target, struck.Ally);
                fx.Burst(Center(state, layout, struck.Target, struck.Ally, motion), 17, struck.Boss ? 5 : 3, Blood, random);
                break;
            case Fell fell:
                fx.Burst(Center(state, layout, fell.Kind, fell.Ally, motion), 21, 8, Dust, random);
                if (fell.Lost > 0)
                {
                    fx.Float($"-{Math.Max(1, Math.Round(fell.Lost * 100)):0} % XP", Center(state, layout, fell.Kind, 0, motion), 10, Palette.Red, 1.2f, -2);
                }

                break;
            case Mended mended:
                fx.Burst(Center(state, layout, mended.Kind, mended.Ally, motion), 15, 2, Mend, random);
                break;
            case Revived revived:
                fx.Burst(Center(state, layout, revived.Kind, revived.Ally, motion), 16, 8, Glitter, random);
                break;
            case Routed:
                motion.Fleeing = target;
                motion.FleeTime = 0;
                fx.Float("L'ÉQUIPE RECULE", layout.HeroX + 6, 1, Palette.Red, 2.2f, 0);
                break;
            case ActiveUsed used:
                fx.Float(Shout(used), Center(state, layout, used.Kind, 0, motion), 4, used.Role == HeroRole.Tank ? Palette.Cyan : Palette.Gold, 0.9f);
                break;
            case AllyArrived arrived:
                scene.Banners.Add(["RENFORT DE TA GUILDE", $"{arrived.Name} ({Roles.Of(arrived.Role).Name})"]);
                fx.Burst(Center(state, layout, FighterKind.Hero, arrived.Id, motion), 14, 14, Glitter, random);
                break;
            case Boosted { Source: "titan" } boosted:
                scene.Banners.Add(["RÉCOMPENSE DU TITAN", $"+{boosted.Gold} % D'OR ET DE DÉGÂTS"], 4);
                break;
            case Boosted boosted:
                scene.Banners.Add(["CONVOI DE TA GUILDE", $"+{boosted.Gold} % D'OR"]);
                break;
        }
    }

    private static string Shout(ActiveUsed used) => used.Role switch
    {
        HeroRole.Warrior => "ÉTOURDI !",
        HeroRole.Tank => "REMPART",
        HeroRole.Archer => "VOLÉE",
        HeroRole.Mage => "DÉFLAGRATION",
        _ => used.Raised > 0 ? "RÉSURRECTION" : "PRIÈRE",
    };

    private static void Hurt(SceneMotion motion, FighterKind kind, long ally)
    {
        if (ally != 0)
        {
            motion.AllyHurt[ally] = 0.1f;
        }
        else
        {
            motion.Hurt[(int)kind] = 0.1f;
        }
    }

    private static int Center(FrontierState state, FrontierLayout layout, FighterKind kind, long ally, SceneMotion motion) =>
        ((ally != 0 ? Formation.AllyX(state, layout, ally) : null) ?? FrontPainter.Walk(state, layout, kind, motion)) + 5;
}
