using System;
using IdleBar.Frontier;
using IdleBar.Ui;

namespace IdleBar.Scene;

public static class CombatReactions
{
    private const float CriticalPause = 0.25f;

    private static readonly Rgba[] Spark = [Palette.Yellow, Palette.White];
    private static readonly Rgba[] Daze = [Palette.Yellow, Palette.Gold];

    public static void Hit(FrontierScene scene, Hit hit, MonsterBox target, FrontierLayout layout, FrontierState state, Random random)
    {
        SceneFx fx = scene.Fx;
        SceneMotion motion = scene.Motion;
        string amount = NumberFormat.Amount(hit.Amount);
        if (hit.Critical && scene.Time - scene.CriticalShown > CriticalPause)
        {
            scene.CriticalShown = scene.Time;
            fx.Float("CRITIQUE", target.CenterX, target.Y - 9, Palette.Gold, 0.9f);
        }

        if (hit.Big && Striker(state, hit) == HeroRole.Warrior)
        {
            fx.Burst(target.CenterX, target.Y, 4, Daze, random);
        }

        switch (hit.Source)
        {
            case HitSource.Ally:
                Assist(scene, hit, target, layout, state, random);
                break;
            case HitSource.Hero when state.Role == HeroRole.Archer:
                fx.Projectiles.Add(new Projectile(false, Formation.X(state, layout, FighterKind.Hero) + 10, 17, target.X + 2, target.MiddleY, 0.12f, hit.Amount));
                break;
            case HitSource.Hero when state.Role == HeroRole.Mage:
                fx.Projectiles.Add(new Projectile(true, Formation.X(state, layout, FighterKind.Hero) + 10, 12, target.CenterX, target.MiddleY, 0.3f, hit.Amount, hit.Big));
                break;
            case HitSource.Hero when state.Role == HeroRole.Support:
                fx.Projectiles.Add(Light(Formation.X(state, layout, FighterKind.Hero), target, hit));
                break;
            case HitSource.Hero:
                motion.HeroSwing = 1;
                motion.MonsterFlash = 0.07f;
                motion.MonsterKick = 0.08f;
                fx.Float(amount, target.CenterX + random.Next(-5, 6), target.Y - 2, Palette.White);
                break;
            case HitSource.First:
                Companion(scene, hit, FighterKind.First, target, layout, state, random);
                break;
            case HitSource.Second:
                Companion(scene, hit, FighterKind.Second, target, layout, state, random);
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
            case HitSource.Judgment:
                motion.MonsterFlash = 0.2f;
                fx.Float("JUGEMENT : " + amount, target.CenterX, 1, Palette.White, 1.6f, 0);
                fx.Burst(target.CenterX, target.Y, 12, Spark, random);
                break;
        }
    }

    private static void Companion(FrontierScene scene, Hit hit, FighterKind kind, MonsterBox target, FrontierLayout layout, FrontierState state, Random random)
    {
        int x = FrontPainter.Walk(state, layout, kind, scene.Motion);
        switch (state.ClassOf(kind))
        {
            case HeroRole.Archer:
                scene.Fx.Projectiles.Add(new Projectile(false, x + 10, 17, target.X + 2, target.MiddleY, 0.16f, hit.Amount));
                break;
            case HeroRole.Mage:
                scene.Fx.Projectiles.Add(new Projectile(true, x + 10, 12, target.CenterX, target.MiddleY, 0.3f, hit.Amount, hit.Big));
                break;
            case HeroRole.Support:
                scene.Fx.Projectiles.Add(Light(x, target, hit));
                break;
            default:
                scene.Motion.CompanionSwing[(int)kind] = 1;
                scene.Motion.MonsterFlash = 0.07f;
                scene.Fx.Float(NumberFormat.Amount(hit.Amount), target.CenterX + random.Next(-5, 6), target.Y - 2, Palette.Silver);
                break;
        }
    }

    private static void Assist(FrontierScene scene, Hit hit, MonsterBox target, FrontierLayout layout, FrontierState state, Random random)
    {
        if (state.Allies.Find(ally => ally.Id == hit.Ally) is not Ally ally || Formation.AllyX(state, layout, ally.Id) is not int x)
        {
            return;
        }

        switch (ally.Role)
        {
            case HeroRole.Archer:
                scene.Fx.Projectiles.Add(new Projectile(false, x + 10, 17, target.X + 2, target.MiddleY, 0.14f, hit.Amount));
                break;
            case HeroRole.Mage:
                scene.Fx.Projectiles.Add(new Projectile(true, x + 10, 12, target.CenterX, target.MiddleY, 0.3f, hit.Amount, hit.Big));
                break;
            case HeroRole.Support:
                scene.Fx.Projectiles.Add(Light(x, target, hit));
                break;
            default:
                scene.Motion.AllySwing[ally.Id] = 1;
                scene.Motion.MonsterFlash = 0.07f;
                scene.Fx.Float(NumberFormat.Amount(hit.Amount), target.CenterX + random.Next(-5, 6), target.Y - 2, Palette.Silver);
                break;
        }
    }

    private static Projectile Light(int x, MonsterBox target, Hit hit) =>
        new(true, x + 10, 12, target.CenterX, target.MiddleY, 0.25f, hit.Amount, holy: true);

    private static HeroRole? Striker(FrontierState state, Hit hit) => hit.Source switch
    {
        HitSource.Hero => state.Role,
        HitSource.First => state.ClassOf(FighterKind.First),
        HitSource.Second => state.ClassOf(FighterKind.Second),
        HitSource.Ally => state.Allies.Find(ally => ally.Id == hit.Ally)?.Role,
        _ => null,
    };

    public static void Bolt(FrontierScene scene, MonsterBox target, Random random, bool deep = false)
    {
        scene.Motion.BoltPath.Clear();
        int x = target.CenterX;
        for (int y = -2; y <= (deep ? target.MiddleY : target.Y + 2); y += 3)
        {
            scene.Motion.BoltPath.Add(new ArtPoint(x, y));
            x = Math.Clamp(x + random.Next(-2, 3), target.CenterX - 4, target.CenterX + 4);
        }
    }
}
