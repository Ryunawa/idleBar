using System;
using System.Collections.Generic;

namespace IdleBar.Scene;

public sealed class SceneFx
{
    private const int MaxFloats = 18;
    private const int MaxParticles = 260;
    private const int MaxCoins = 90;

    public List<FloatText> Floats { get; } = [];

    public List<Particle> Particles { get; } = [];

    public List<FlyingCoin> Coins { get; } = [];

    public List<Projectile> Projectiles { get; } = [];

    public void Float(string text, float x, float y, Rgba color, float life = 0.8f, float rise = -10)
    {
        Floats.Add(new FloatText(text, x, y, color, life, rise));
        if (Floats.Count > MaxFloats)
        {
            Floats.RemoveAt(0);
        }
    }

    public void Add(Particle particle)
    {
        Particles.Add(particle);
        if (Particles.Count > MaxParticles)
        {
            Particles.RemoveAt(0);
        }
    }

    public void Burst(float x, float y, int count, IReadOnlyList<Rgba> colors, Random random)
    {
        for (int index = 0; index < count; index++)
        {
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float speed = 14 + (float)random.NextDouble() * 18;
            Add(new Particle(
                ParticleKind.Dot,
                x,
                y,
                MathF.Cos(angle) * speed,
                MathF.Sin(angle) * speed - 4,
                -6,
                0.35f + (float)random.NextDouble() * 0.2f,
                colors[random.Next(colors.Count)]));
        }
    }

    public void Spill(float x, float y, int count, Random random)
    {
        for (int index = 0; index < count; index++)
        {
            Coins.Add(new FlyingCoin(
                x,
                y,
                ((float)random.NextDouble() - 0.5f) * 34,
                -24 - (float)random.NextDouble() * 20,
                0.3f + (float)random.NextDouble() * 0.18f,
                0.8f + (float)random.NextDouble() * 0.4f));
        }

        if (Coins.Count > MaxCoins)
        {
            Coins.RemoveRange(0, Coins.Count - MaxCoins);
        }
    }

    public int Update(float dt, List<Projectile> landed)
    {
        foreach (FloatText text in Floats)
        {
            text.Life -= dt;
            text.Y += text.Rise * dt;
        }

        Floats.RemoveAll(text => text.Life <= 0);
        foreach (Particle particle in Particles)
        {
            particle.Life -= dt;
            particle.X += particle.SpeedX * dt;
            particle.Y += particle.SpeedY * dt;
            particle.SpeedY += particle.Gravity * dt;
        }

        Particles.RemoveAll(particle => particle.Life <= 0 || particle.Y > 30);
        foreach (FlyingCoin coin in Coins)
        {
            coin.Update(dt);
        }

        int arrived = Coins.RemoveAll(coin => coin.Landed);
        foreach (Projectile projectile in Projectiles)
        {
            projectile.Time += dt;
            if (!projectile.Landed && projectile.Time >= projectile.Duration)
            {
                projectile.Landed = true;
                landed.Add(projectile);
            }
        }

        Projectiles.RemoveAll(projectile => projectile.Landed);
        return arrived;
    }
}
