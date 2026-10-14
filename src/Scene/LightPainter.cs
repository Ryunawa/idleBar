using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class LightPainter
{
    private static readonly Rgba[] Swirl = [Palette.Plum, Palette.Pink, Palette.Cyan, Palette.NavyBlue];

    public static void Paint(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene, bool lit)
    {
        float time = scene.Time;
        if (!layout.Narrow)
        {
            for (int index = 0; index < FrontierLayout.Slots; index++)
            {
                if (state.Built[index] is BuildingKind kind && scene.Motion.Build[index] is null)
                {
                    Building(r, kind, layout.SlotCenter(index), Economy.Tier(state.Levels[index]), lit, time);
                }
            }
        }

        float swing = scene.Motion.HeroSwing;
        if (state.Role == HeroRole.Warrior && swing > 0.15f && swing < 0.85f)
        {
            float end = FrontPainter.SwordAngle(scene.Motion, time);
            int hero = Formation.X(state, layout, FighterKind.Hero);
            for (float angle = -2.0f; angle < end; angle += 0.12f)
            {
                r.Set((int)MathF.Round(hero + 9 + MathF.Cos(angle) * 10), (int)MathF.Round(FrontierLayout.Ground - HeroGrids.Height + HeroGrids.HandY + MathF.Sin(angle) * 10), Palette.White.WithAlpha(0.6f));
            }
        }
    }

    private static void Building(ArtRaster r, BuildingKind kind, int cx, int tier, bool lit, float time)
    {
        switch (kind)
        {
            case BuildingKind.Farm when lit:
                r.Set(cx - 4, 16, Palette.Yellow);
                break;
            case BuildingKind.Mill when lit:
                r.Fill(cx - 1, 13, 2, 2, Palette.Gold);
                break;
            case BuildingKind.Bakery when lit:
                r.Fill(cx - 8, 17, 6, 4, Palette.Gold);
                r.Fill(cx - 7, 19, 4, 1, Palette.Yellow);
                break;
            case BuildingKind.Forge:
                float flicker = MathF.Sin(time * 9) + MathF.Sin(time * 13.7f);
                r.Fill(cx - 8, 18, 6, 6, flicker > 0.6f ? Palette.Gold : Palette.Orange);
                r.Fill(cx - 7, 22, 4, 2, Palette.Yellow);
                break;
            case BuildingKind.Market when lit:
                r.Set(cx - 17, 12, Palette.Yellow);
                r.Set(cx + 16, 12, Palette.Yellow);
                break;
            case BuildingKind.Tower:
                r.Fill(cx - 1, 9, 3, 3, MathF.Sin(time * 2.4f) > -0.2f ? Palette.Cyan : Palette.Blue);
                if (lit)
                {
                    r.Fill(cx - 1, 15, 3, 2, Palette.Gold);
                }

                if (MathF.Sin(time * 5.3f) > 0.85f)
                {
                    r.Set(cx + (int)MathF.Round(MathF.Sin(time * 7) * 4), (int)MathF.Round(1 + MathF.Cos(time * 5) * 1.5f), Palette.Yellow);
                }

                break;
            case BuildingKind.Portal:
                Portal(r, cx, time);
                break;
            case BuildingKind.Barracks when lit:
                r.Set(cx - 4, 18, MathF.Sin(time * 11) > 0 ? Palette.Gold : Palette.Orange);
                r.Set(cx + 4, 18, MathF.Sin(time * 9) > 0 ? Palette.Gold : Palette.Orange);
                break;
            case BuildingKind.Chapel when lit:
                r.Fill(cx - 1, 14, 3, 2, Palette.Gold);
                break;
            case BuildingKind.Tavern when lit:
                r.Fill(cx - 8, 17, 4, 3, Palette.Gold);
                r.Fill(cx + 5, 17, 4, 3, Palette.Gold);
                r.Fill(cx - 6, 10, 3, 3, Palette.Yellow);
                break;
            case BuildingKind.Workshop when lit:
                r.Fill(cx - 8, 16, 11, 3, Palette.Rust);
                break;
            case BuildingKind.Belfry when lit:
                r.Fill(cx - 1, 4, 3, 2, Palette.Yellow);
                break;
        }

        if (tier >= 5 && MathF.Sin(time * 3.1f + cx) > 0.8f)
        {
            r.Set(cx + (int)MathF.Round(MathF.Sin(time * 4.3f + cx) * 9), 6 + (int)MathF.Round(MathF.Cos(time * 3.7f) * 3), Palette.White);
        }
    }

    private static void Portal(ArtRaster r, int cx, float time)
    {
        for (int y = 9; y < FrontierLayout.Ground; y++)
        {
            for (int x = cx - 7; x <= cx + 7; x++)
            {
                float dx = (x - cx) / 7.5f;
                float dy = (y - 16) / 7.5f;
                if (dx * dx + dy * dy > 1)
                {
                    continue;
                }

                float distance = MathF.Sqrt(dx * dx + dy * dy) * 7.5f;
                int index = (((int)MathF.Floor(distance * 0.9f - time * 4 + MathF.Atan2(dy, dx) * 1.5f) % 4) + 4) % 4;
                r.Set(x, y, Swirl[index]);
            }
        }
    }
}
