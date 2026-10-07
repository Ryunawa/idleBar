using System;
using Godot;

namespace IdleBar.Pixel;

public static class FolkPainter
{
    private const float WalkSpeed = 3.5f;
    private const float PauseSeconds = 2.5f;
    private const float EveningDaylight = 0.3f;
    private const float TrafficPeriod = 70f;
    private const float WalkerPace = 3f;
    private const float CartPace = 4f;
    private const int TrafficKinds = 3;
    private const float FlockPeriod = 55f;
    private const float BirdSpeed = 12f;
    private const float FlapsPerSecond = 6f;
    private const int FireflySpread = 45;
    private const float FireflyBlinkSeconds = 2.8f;
    private const float ErrandPhase = 5f;
    private const int ErrandReach = 18;
    private const float CoinRise = 3f;

    private static readonly Color Harness = new("4e331d");
    private static readonly Color Firefly = new("d8f27e");
    private static readonly Color Coin = new("f2c14e");

    public static void PaintVillagers(PixelCanvas canvas, int start, int width, Ambience ambience)
    {
        int feet = LandscapePainter.GroundTop(canvas) + 1;
        Stroll(canvas, FolkSprites.Baker, start - 4, start + width - 10, feet, ambience.Time);
        if (ambience.Light.Daylight > EveningDaylight)
        {
            Stroll(canvas, FolkSprites.Farmer, start + 12, start + width + 4, feet, ambience.Time + 11);
        }
    }

    public static void PaintTraffic(PixelCanvas canvas, float time, float roadSpeed)
    {
        int slot = (int)(time / TrafficPeriod);
        float elapsed = time - slot * TrafficPeriod;
        int kind = LandscapePainter.Scatter(slot + 3000) % TrafficKinds;
        float speed = roadSpeed + (kind == 2 ? CartPace : WalkerPace);
        int x = canvas.Width + 2 - (int)(elapsed * speed);
        int feet = LandscapePainter.GroundTop(canvas) + 1;
        switch (kind)
        {
            case 0:
                Walk(canvas, FolkSprites.Pilgrim, x, feet, time);
                break;
            case 1:
                Walk(canvas, FolkSprites.Peddler, x, feet, time);
                break;
            default:
                PaintCart(canvas, x, feet, time);
                break;
        }
    }

    public static void PaintBirds(PixelCanvas canvas, Ambience ambience)
    {
        if (ambience.Light.Daylight < 0.5f)
        {
            return;
        }

        float time = ambience.Time;
        int flock = (int)(time / FlockPeriod);
        int seed = LandscapePainter.Scatter(flock + 4000);
        int lead = (int)((time - flock * FlockPeriod) * BirdSpeed) - 6;
        bool leftward = seed % 2 == 0;
        int height = 2 + seed % Math.Max(LandscapePainter.GroundTop(canvas) / 3, 1);
        for (int bird = 0; bird < 2 + seed % 3; bird++)
        {
            int along = lead - bird * 6;
            if (along < -6 || along > canvas.Width + 6)
            {
                continue;
            }

            int y = height + bird % 2 * 2 + (int)MathF.Round(MathF.Sin(time * 2 + bird));
            PixelSprite wings = ((int)(time * FlapsPerSecond) + bird) % 2 == 0 ? FolkSprites.BirdUp : FolkSprites.BirdDown;
            canvas.Draw(wings, leftward ? canvas.Width - along : along, y);
        }
    }

    public static void PaintFireflies(PixelCanvas canvas, BiomeStyle style, float distance, Ambience ambience)
    {
        float glow = Math.Clamp(1 - ambience.Light.Daylight * 4, 0, 1);
        if (!style.Fireflies || glow <= 0)
        {
            return;
        }

        float time = ambience.Time;
        int ground = LandscapePainter.GroundTop(canvas);
        int span = canvas.Width + 20;
        int drift = (int)(distance * 0.75f);
        for (int index = 0; index < canvas.Width / FireflySpread + 1; index++)
        {
            float blink = (time + index * 0.73f) % FireflyBlinkSeconds / FireflyBlinkSeconds;
            if (blink >= 0.55f)
            {
                continue;
            }

            float alpha = MathF.Sin(blink / 0.55f * MathF.PI) * glow;
            int home = ((LandscapePainter.Scatter(index + 300) - drift) % span + span) % span - 10;
            int x = home + (int)MathF.Round(MathF.Sin(time * 0.6f + index * 1.7f) * 5);
            int y = ground - 2 - (int)MathF.Round((MathF.Sin(time * 0.9f + index * 2.3f) + 1) * 3);
            canvas.Fill(x - 1, y - 1, 3, 3, Firefly with { A = 0.15f * alpha });
            canvas.Fill(x, y, 1, 1, Firefly with { A = alpha });
        }
    }

    public static void PaintErrand(PixelCanvas canvas, int start, int width, Ambience ambience)
    {
        float time = ambience.Time + ErrandPhase;
        int feet = LandscapePainter.GroundTop(canvas) + 1;
        Patrol patrol = Patrol.At(start - ErrandReach, start + width - ErrandReach, time, WalkSpeed, PauseSeconds);
        Walker walker = patrol.Leftward ? FolkSprites.Courier : FolkSprites.Porter;
        PixelSprite pose = walker.Pose(patrol.Moving, time);
        int x = (int)patrol.X;
        canvas.Draw(patrol.Leftward ? pose.Mirrored : pose, x, feet - pose.Height + 1);
        if (patrol is { Moving: false, Leftward: true })
        {
            float lift = patrol.Pause / PauseSeconds;
            canvas.Fill(x + 2, feet - pose.Height - 1 - (int)(lift * CoinRise), 1, 1, Coin with { A = 1 - lift });
        }
    }

    private static void Stroll(PixelCanvas canvas, Walker walker, int from, int to, int feet, float time)
    {
        Patrol patrol = Patrol.At(from, to, time, WalkSpeed, PauseSeconds);
        PixelSprite pose = walker.Pose(patrol.Moving, time);
        canvas.Draw(patrol.Leftward ? pose.Mirrored : pose, (int)patrol.X, feet - pose.Height + 1);
    }

    private static void Walk(PixelCanvas canvas, Walker walker, int x, int feet, float time)
    {
        PixelSprite pose = walker.Pose(true, time).Mirrored;
        canvas.Draw(pose, x, feet - pose.Height + 1);
    }

    private static void PaintCart(PixelCanvas canvas, int x, int feet, float time)
    {
        bool stepping = (int)(time / 0.2f) % 2 == 0;
        PixelSprite ox = (stepping ? CaravanSprites.OxStepping : CaravanSprites.OxStriding).Mirrored;
        PixelSprite wagon = CaravanSprites.Wagon(stepping, CaravanLook.Plain).Mirrored;
        canvas.Draw(ox, x, feet - ox.Height + 1);
        canvas.Fill(x + ox.Width - 2, feet - 4, 4, 1, Harness);
        canvas.Draw(wagon, x + ox.Width + 1, feet - wagon.Height + 1);
    }
}