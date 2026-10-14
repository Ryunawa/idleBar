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
            case GoldRained rained:
                motion.Rain = 2.5f;
                fx.Float("+" + NumberFormat.Amount(rained.Gold), layout.VillageX + 30, 3, Palette.Yellow, 1.6f, 0);
                break;
            case Smote:
                motion.Lightning = 0.35f;
                motion.Shake = 0.25f;
                CombatReactions.Bolt(scene, scene.LastMonster ?? new MonsterBox(0, false, 1, layout.MonsterX, 12, 12, 12), random);
                break;
            case FuryStarted:
                fx.Float("FURIE !", layout.HeroX + 5, 3, Palette.Red, 1.4f, 0);
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
}
