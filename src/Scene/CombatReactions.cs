using System;
using IdleBar.Frontier;
using IdleBar.Ui;

namespace IdleBar.Scene;

public static class CombatReactions
{
    private static readonly Rgba[] Spark = [Palette.Yellow, Palette.White];

    public static void Hit(FrontierScene scene, Hit hit, MonsterBox target, FrontierLayout layout, FrontierState state, Random random)
    {
        SceneFx fx = scene.Fx;
        SceneMotion motion = scene.Motion;
        string amount = NumberFormat.Amount(hit.Amount);
        if (hit.Critical)
        {
            fx.Float("CRITIQUE", target.CenterX, target.Y - 9, Palette.Gold, 0.9f);
        }

        switch (hit.Source)
        {
            case HitSource.Ally:
                Assist(scene, hit, target, layout, state, random);
                break;
            case HitSource.Hero when state.Role == HeroRole.Archer:
                fx.Projectiles.Add(new Projectile(false, layout.HeroX + 10, 17, target.X + 2, target.MiddleY, 0.12f, hit.Amount));
                break;
            case HitSource.Hero when state.Role == HeroRole.Mage:
                fx.Projectiles.Add(new Projectile(true, layout.HeroX + 10, 12, target.CenterX, target.MiddleY, 0.3f, hit.Amount));
                break;
            case HitSource.Hero:
                motion.HeroSwing = 1;
                motion.MonsterFlash = 0.07f;
                motion.MonsterKick = 0.08f;
                fx.Float(amount, target.CenterX + random.Next(-5, 6), target.Y - 2, Palette.White);
                break;
            case HitSource.Archer:
                fx.Projectiles.Add(new Projectile(false, layout.ArcherX + 8, 17, target.X + 2, target.MiddleY, 0.16f, hit.Amount));
                break;
            case HitSource.Mage:
                motion.MageGlow = 0.25f;
                fx.Projectiles.Add(new Projectile(true, layout.MageX + 7, 16, target.CenterX, target.MiddleY, 0.3f, hit.Amount));
                break;
            case HitSource.Click:
                ArtPoint strike = scene.LastStrike ?? new ArtPoint(target.CenterX, target.Y + 4);
                motion.MonsterFlash = 0.07f;
                fx.Float(amount, strike.X, target.Y - 2, Palette.Yellow);
                fx.Burst(strike.X, strike.Y, 5, Spark, random);
                break;
            case HitSource.Smite:
                motion.MonsterFlash = 0.14f;
                fx.Float(amount + " !", target.CenterX, target.Y - 3, Palette.Gold, 1.2f);
                break;
        }
    }

    private static void Assist(FrontierScene scene, Hit hit, MonsterBox target, FrontierLayout layout, FrontierState state, Random random)
    {
        if (state.Allies.Find(ally => ally.Id == hit.Ally) is not Ally ally || TeamPainter.AllyX(state, layout, ally.Id) is not int x)
        {
            return;
        }

        switch (ally.Role)
        {
            case HeroRole.Archer:
                scene.Fx.Projectiles.Add(new Projectile(false, x + 10, 17, target.X + 2, target.MiddleY, 0.14f, hit.Amount));
                break;
            case HeroRole.Mage:
                scene.Fx.Projectiles.Add(new Projectile(true, x + 10, 12, target.CenterX, target.MiddleY, 0.3f, hit.Amount));
                break;
            default:
                scene.Motion.AllySwing[ally.Id] = 1;
                scene.Motion.MonsterFlash = 0.07f;
                scene.Fx.Float(NumberFormat.Amount(hit.Amount), target.CenterX + random.Next(-5, 6), target.Y - 2, Palette.Silver);
                break;
        }
    }

    public static void Bolt(FrontierScene scene, MonsterBox target, Random random)
    {
        scene.Motion.BoltPath.Clear();
        int x = target.CenterX;
        for (int y = -2; y <= target.Y + 2; y += 3)
        {
            scene.Motion.BoltPath.Add(new ArtPoint(x, y));
            x = Math.Clamp(x + random.Next(-2, 3), target.CenterX - 4, target.CenterX + 4);
        }
    }
}
