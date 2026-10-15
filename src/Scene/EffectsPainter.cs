using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class EffectsPainter
{
    public static void Projectiles(ArtRaster r, SceneFx fx)
    {
        foreach (Projectile projectile in fx.Projectiles)
        {
            float progress = projectile.Progress;
            if (!projectile.Magic)
            {
                float x = projectile.FromX + (projectile.ToX - projectile.FromX) * progress;
                float y = projectile.FromY + (projectile.ToY - projectile.FromY) * progress - MathF.Sin(progress * MathF.PI) * 2;
                r.Line((int)MathF.Round(x - 4), (int)MathF.Round(y), (int)MathF.Round(x - 1), (int)MathF.Round(y), Palette.Sand);
                r.Set((int)MathF.Round(x), (int)MathF.Round(y), Palette.Silver);
                continue;
            }

            for (int trail = 2; trail >= 0; trail--)
            {
                float step = Math.Max(0, progress - trail * 0.08f);
                int x = (int)MathF.Round(projectile.FromX + (projectile.ToX - projectile.FromX) * step);
                int y = (int)MathF.Round(projectile.FromY + (projectile.ToY - projectile.FromY) * step - MathF.Sin(step * MathF.PI) * 4);
                if (trail > 0)
                {
                    r.Set(x, y, projectile.Holy ? (trail == 1 ? Palette.Yellow : Palette.Gold) : (trail == 1 ? Palette.Cyan : Palette.Blue));
                    continue;
                }

                r.Set(x, y, Palette.White);
                if (!projectile.Big)
                {
                    continue;
                }

                r.Set(x - 1, y, Palette.Cyan);
                r.Set(x + 1, y, Palette.Cyan);
                r.Set(x, y - 1, Palette.Cyan);
                r.Set(x, y + 1, Palette.Cyan);
                r.Set(x - 2, y, Palette.Blue);
                r.Set(x + 2, y, Palette.Blue);
                r.Set(x, y - 2, Palette.Blue);
                r.Set(x, y + 2, Palette.Blue);
            }
        }
    }

    public static void Lightning(ArtRaster r, SceneMotion motion, FrontierLayout layout, float time)
    {
        if (motion.Lightning <= 0 || motion.BoltPath.Count < 2)
        {
            return;
        }

        if (motion.Lightning > 0.24f)
        {
            r.Fill(layout.FrontX, -layout.Top, layout.FogX - layout.FrontX, FrontierLayout.Ground + layout.Top, Rgba.Hex("fffbe0").WithAlpha(0.35f));
        }

        Bolt(r, motion, time);
    }

    public static void Judgment(ArtRaster r, SceneMotion motion, float time)
    {
        if (motion.Heavy && motion.Lightning > 0 && motion.BoltPath.Count >= 2)
        {
            Bolt(r, motion, time);
        }
    }

    private static void Bolt(ArtRaster r, SceneMotion motion, float time)
    {
        Rgba core = (int)(time * 30) % 2 == 0 ? Palette.White : Palette.Yellow;
        int width = motion.Heavy ? 2 : 1;
        for (int index = 1; index < motion.BoltPath.Count; index++)
        {
            ArtPoint from = motion.BoltPath[index - 1];
            ArtPoint to = motion.BoltPath[index];
            r.Line(from.X + width, from.Y, to.X + width, to.Y, Palette.Gold);
            if (motion.Heavy)
            {
                r.Line(from.X - 1, from.Y, to.X - 1, to.Y, Palette.Gold);
            }

            for (int offset = 0; offset < width; offset++)
            {
                r.Line(from.X + offset, from.Y, to.X + offset, to.Y, core);
            }
        }
    }

    public static void Storm(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene)
    {
        const float Every = 0.9f;
        if (state.StormLeft <= 0 || scene.Box(state, layout) is not MonsterBox box || scene.Time % Every > 0.12f)
        {
            return;
        }

        int strike = (int)(scene.Time / Every);
        int x = box.CenterX + strike % 3 * 4 - 4;
        Rgba core = strike % 2 == 0 ? Palette.White : Palette.Yellow;
        for (int y = -2; y < box.Y + 2; y += 3)
        {
            int next = x + (y / 3 % 2 == 0 ? 1 : -1);
            r.Line(x + 1, y, next + 1, y + 3, Palette.Gold);
            r.Line(x, y, next, y + 3, core);
            x = next;
        }
    }

    public static void Particles(ArtRaster r, SceneFx fx)
    {
        foreach (Particle particle in fx.Particles)
        {
            int x = (int)MathF.Round(particle.X);
            int y = (int)MathF.Round(particle.Y);
            float share = particle.Life / particle.Max;
            switch (particle.Kind)
            {
                case ParticleKind.Smoke:
                    int size = share > 0.55f ? 2 : 1;
                    r.Fill(x, y, size, size, particle.Color.WithAlpha(0.75f * share));
                    break;
                case ParticleKind.Coin:
                    r.Fill(x, y, 2, 2, Palette.Gold.WithAlpha(Math.Min(1, share / 0.4f)));
                    r.Set(x, y, Palette.Yellow.WithAlpha(Math.Min(1, share / 0.4f)));
                    break;
                default:
                    r.Set(x, y, particle.Color.WithAlpha(Math.Min(1, share / 0.4f)));
                    break;
            }
        }
    }

    public static void Coins(ArtRaster r, SceneFx fx)
    {
        foreach (FlyingCoin coin in fx.Coins)
        {
            int x = (int)MathF.Round(coin.X);
            int y = (int)MathF.Round(coin.Y);
            r.Fill(x, y, 2, 2, Palette.Gold);
            r.Set(x, y, Palette.Yellow);
        }
    }

    public static void Flash(ArtRaster r, SceneMotion motion, FrontierLayout layout)
    {
        if (motion.Flash > 0)
        {
            r.Fill(0, -layout.Top, layout.Width, FrontierLayout.Rows + layout.Top, Palette.White.WithAlpha(Math.Min(1, motion.Flash / 0.6f) * 0.8f));
        }
    }
}
