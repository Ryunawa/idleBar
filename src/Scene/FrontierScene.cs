using System;
using System.Collections.Generic;
using IdleBar.Frontier;
using IdleBar.Ui;

namespace IdleBar.Scene;

public sealed class FrontierScene
{
    private const float ReplayPace = 8;
    private const float TimeWrap = 3600;

    private static readonly Rgba[] MagicSparks = [Palette.Cyan, Palette.White];
    private static readonly Rgba[] HolySparks = [Palette.Orange, Palette.White];

    private readonly Random _random;

    public FrontierScene(Random random)
    {
        _random = random;
    }

    public float Time { get; private set; }

    public float CriticalShown { get; set; } = -1;

    public SceneFx Fx { get; } = new();

    public SceneMotion Motion { get; } = new();

    public VillageLife Village { get; } = new();

    public BannerQueue Banners { get; } = new();

    public MonsterBox? LastMonster { get; private set; }

    public ArtPoint LastChicken { get; private set; }

    public ArtPoint? LastStrike { get; set; }

    public int Update(float dt, FrontierState state, IReadOnlyList<FrontierEvent> events, FrontierLayout layout, bool night)
    {
        float pace = Motion.Replay > 0 ? ReplayPace : 1;
        Time = (Time + dt * pace) % TimeWrap;
        Motion.Update(dt);
        foreach (FrontierEvent frontierEvent in events)
        {
            SceneReactions.Apply(this, frontierEvent, state, layout, _random);
        }

        List<Projectile> landed = [];
        int arrived = Fx.Update(dt, landed);
        foreach (Projectile projectile in landed)
        {
            Motion.MonsterFlash = 0.06f;
            Fx.Float(NumberFormat.Amount(projectile.Amount), projectile.ToX + _random.Next(-4, 5), (LastMonster?.Y ?? 12) - 2, projectile.Holy ? Palette.Orange : projectile.Magic ? Palette.Cyan : Palette.Green);
            if (projectile.Magic)
            {
                Fx.Burst(projectile.ToX, projectile.ToY, 6, projectile.Holy ? HolySparks : MagicSparks, _random);
            }
        }

        Village.Update(dt * pace, state, layout, Fx, _random, night);
        Banners.Update(dt);
        Trails(dt, state, layout);
        LastMonster = Box(state, layout) ?? LastMonster;
        if (state.Chicken is not null)
        {
            LastChicken = Chicken(state, layout);
        }

        return arrived;
    }

    public MonsterBox? Box(FrontierState state, FrontierLayout layout) =>
        state.Monster is Monster monster ? MonsterBox.Of(monster, layout, Motion.MonsterShift) : null;

    public ArtPoint Chicken(FrontierState state, FrontierLayout layout)
    {
        double progress = GoldenChicken.Progress(state);
        int x = (int)Math.Round(layout.SkillsX - 12 - (layout.SkillsX - 12 - layout.VillageX) * progress);
        int y = (int)Math.Round(2 + Math.Sin((state.Chicken ?? 0) * 2.4) * 1.5);
        return new ArtPoint(x, y);
    }

    private void Trails(float dt, FrontierState state, FrontierLayout layout)
    {
        if (state.Chicken is not null)
        {
            ArtPoint chicken = Chicken(state, layout);
            Rgba color = _random.Next(2) == 0 ? Palette.Yellow : Palette.White;
            Fx.Add(new Particle(ParticleKind.Dot, chicken.X + 8, chicken.Y + 3 + _random.Next(3), 8, 0, 0, 0.45f, color));
        }

        if (Motion.Rain > 0)
        {
            for (int drop = 0; drop < 2; drop++)
            {
                float x = layout.VillageX + (float)_random.NextDouble() * (layout.SkillsX - layout.VillageX);
                Fx.Add(new Particle(ParticleKind.Coin, x, -1, 0, 16 + (float)_random.NextDouble() * 10, 24, 1.4f, Palette.Gold));
            }
        }
    }
}
