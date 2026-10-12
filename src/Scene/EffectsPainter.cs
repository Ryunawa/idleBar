using System;

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
                    r.Set(x, y, trail == 1 ? Palette.Cyan : Palette.Blue);
                    continue;
                }

                r.Set(x, y, Palette.White);
                r.Set(x - 1, y, Palette.Cyan);
                r.Set(x + 1, y, Palette.Cyan);
                r.Set(x, y - 1, Palette.Cyan);
                r.Set(x, y + 1, Palette.Cyan);
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

        Rgba core = (int)(time * 30) % 2 == 0 ? Palette.White : Palette.Yellow;
        for (int index = 1; index < motion.BoltPath.Count; index++)
        {
            ArtPoint from = motion.BoltPath[index - 1];
            ArtPoint to = motion.BoltPath[index];
            r.Line(from.X + 1, from.Y, to.X + 1, to.Y, Palette.Gold);
            r.Line(from.X, from.Y, to.X, to.Y, core);
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
