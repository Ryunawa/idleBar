using System;
using IdleBar.Frontier;
using IdleBar.Ui;

namespace IdleBar.Scene;

public static class SkillReactions
{
    private static readonly Rgba[] Glitter = [Palette.Yellow, Palette.Gold, Palette.White];

    public static void Apply(FrontierScene scene, FrontierEvent frontierEvent, FrontierState state, FrontierLayout layout, Random random)
    {
        SceneFx fx = scene.Fx;
        SceneMotion motion = scene.Motion;
        switch (frontierEvent)
        {
            case FrenzyStarted:
                fx.Float("OR ×7 !", scene.LastChicken.X + 4, scene.LastChicken.Y + 2, Palette.Yellow, 1.6f, 0);
                fx.Burst(scene.LastChicken.X + 4, scene.LastChicken.Y + 4, 16, Glitter, random);
                break;
            case SkillCast cast:
                Cast(scene, cast, state, layout, random);
                break;
            case PointEarned earned:
                scene.Banners.Add(["POINT DE COMPÉTENCE", $"{earned.Points} EN TOUT : ONGLET COMPÉTENCES"]);
                int middle = layout.SkillSize / 2;
                fx.Burst(SkillPainter.ButtonX(layout, 1) + middle, SkillPainter.ButtonY(layout) + middle, 14, Glitter, random);
                break;
            case Retreated retreated:
                motion.Flash = 0.6f;
                motion.Forget();
                scene.Banners.Add([$"REPLI : +{retreated.Souls} ÂMES", $"DÉGÂTS ET OR ×{NumberFormat.Factor(retreated.Multiplier)}"], 4);
                break;
            case AchievementUnlocked unlocked:
                scene.Banners.Add(["SUCCÈS DÉBLOQUÉ", unlocked.Achievement.Name]);
                break;
            case Returned returned when returned.Seconds >= 60:
                motion.Replay = 2;
                motion.ReplayLabel = $">> {SceneText.Duration(returned.Seconds)} PLUS TARD";
                scene.Banners.Add([$"PENDANT TON ABSENCE ({SceneText.Duration(returned.Seconds)})", "+" + NumberFormat.Amount(returned.Gold) + " OR"], 4.5f);
                fx.Spill(layout.VillageX + 60, 12, 24, random);
                break;
        }
    }

    private static void Cast(FrontierScene scene, SkillCast cast, FrontierState state, FrontierLayout layout, Random random)
    {
        SceneFx fx = scene.Fx;
        SceneMotion motion = scene.Motion;
        MonsterBox target = scene.LastMonster ?? new MonsterBox(0, false, 1, layout.MonsterX, 12, 12, 12);
        int team = layout.ArcherX + 5;
        switch (cast.Kind)
        {
            case SkillKind.Smite:
                motion.Lightning = 0.35f;
                motion.Shake = 0.25f;
                CombatReactions.Bolt(scene, target, random);
                break;
            case SkillKind.Judgment:
                motion.Lightning = 0.5f;
                motion.Heavy = true;
                motion.Shake = 0.45f;
                CombatReactions.Bolt(scene, target, random, true);
                break;
            case SkillKind.Storm:
                motion.Lightning = 0.3f;
                CombatReactions.Bolt(scene, target, random);
                fx.Float("TEMPÊTE !", target.CenterX, 1, Palette.Yellow, 1.4f, 0);
                break;
            case SkillKind.Fury:
                fx.Float("FURIE !", layout.HeroX + 5, 3, Palette.Red, 1.4f, 0);
                break;
            case SkillKind.HolyShield:
                fx.Float("BOUCLIER SACRÉ", team, 3, Palette.Yellow, 1.4f, 0);
                break;
            case SkillKind.Rally:
                fx.Float("RALLIEMENT !", team, 3, Palette.Cyan, 1.4f, 0);
                fx.Burst(team, 14, 10, Glitter, random);
                break;
            case SkillKind.GoldRain:
                motion.Rain = 2.5f;
                fx.Float("+" + NumberFormat.Amount(cast.Amount), layout.VillageX + 30, 3, Palette.Yellow, 1.6f, 0);
                break;
            case SkillKind.Harvest:
                fx.Float($"MOISSON : OR ×{Balance.HarvestGold:0}", target.CenterX, 1, Palette.Yellow, 1.4f, 0);
                fx.Burst(target.CenterX, target.MiddleY, 12, Glitter, random);
                break;
            case SkillKind.Lure:
                ArtPoint chicken = scene.Chicken(state, layout);
                fx.Float("COT COT !", chicken.X - 8, chicken.Y + 13, Palette.Yellow, 1.4f, 0);
                break;
        }
    }
}
