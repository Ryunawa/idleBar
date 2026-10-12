using System;
using IdleBar.Frontier;
using IdleBar.Ui;

namespace IdleBar.Scene;

public static class SceneReactions
{
    private static readonly Rgba[] Dust = [Palette.Sand, Palette.Peach, Palette.Wood];
    private static readonly Rgba[] Smoke = [Palette.White, Palette.Silver, Palette.Gray];
    private static readonly Rgba[] Glitter = [Palette.Yellow, Palette.Gold, Palette.White];

    public static void Apply(FrontierScene scene, FrontierEvent frontierEvent, FrontierState state, FrontierLayout layout, Random random)
    {
        MonsterBox target = scene.LastMonster ?? new MonsterBox(0, false, 1, layout.MonsterX, 12, 12, 12);
        SceneFx fx = scene.Fx;
        SceneMotion motion = scene.Motion;
        switch (frontierEvent)
        {
            case Hit hit:
                CombatReactions.Hit(scene, hit, target, layout, random);
                break;
            case Killed killed:
                motion.Corpse = target;
                motion.CorpseTime = 0;
                fx.Burst(target.CenterX, target.MiddleY, target.Boss ? 22 : 10, Smoke, random);
                fx.Spill(target.CenterX, target.MiddleY, target.Boss ? 14 : 4, random);
                fx.Float("+" + NumberFormat.Amount(killed.Gold), target.CenterX, target.Y - 8, Palette.Yellow, 1.1f, -6);
                if (killed.Boss)
                {
                    fx.Float("BOSS VAINCU !", layout.HeroX + 10, 1, Palette.Gold, 1.8f, 0);
                }

                break;
            case BossFailed:
                motion.Fleeing = target;
                motion.FleeTime = 0;
                fx.Float("TROP LENT ! ON S'ENTRAÎNE", layout.HeroX + 6, 1, Palette.Red, 2.2f, 0);
                break;
            case BossCalled:
                motion.Fleeing = target;
                motion.FleeTime = 0;
                break;
            case StageReached { Boss: true }:
                fx.Float("BOSS !", layout.MonsterX + 6, 3, Palette.Red, 1.4f, 0);
                break;
            case SlotConquered conquered:
                motion.Reveal[(int)conquered.Kind] = 0;
                scene.Banners.Add(["TERRITOIRE CONQUIS", Buildings.Of(conquered.Kind).Name + " : emplacement libre"]);
                break;
            case Built { Level: 1 } built:
                motion.Build[(int)built.Kind] = 0;
                fx.Burst(layout.SlotCenter(built.Kind), 22, 12, Dust, random);
                break;
            case Built built:
                fx.Float("+1", layout.SlotCenter(built.Kind), 8, Palette.Yellow, 0.7f);
                break;
            case TierReached tier:
                fx.Float($"PALIER {tier.Tier}", layout.SlotCenter(tier.Kind), 4, Palette.Gold, 1.6f, -4);
                fx.Burst(layout.SlotCenter(tier.Kind), 10, 14, Glitter, random);
                break;
            case Trained { Level: 1, Kind: not FighterKind.Hero } trained:
                motion.Recruit[(int)trained.Kind] = 0;
                fx.Float(Fighters.Of(trained.Kind).Name + " !", layout.FighterX(trained.Kind) + 4, 3, Palette.Green, 1.5f, 0);
                break;
            case Trained trained:
                fx.Float("+1", layout.FighterX(trained.Kind) + 4, 7, Palette.Yellow, 0.7f);
                break;
            case Supplied:
                fx.Float("+5 %", layout.HeroX + 5, 8, Palette.Green, 0.9f);
                break;
            default:
                SkillReactions.Apply(scene, frontierEvent, state, layout, random);
                break;
        }
    }
}
