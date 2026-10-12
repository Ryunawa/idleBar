using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class BuildingPainter
{
    private static readonly int[] PennantX = [-1, 9, -3, -6, -1, 6, 0];
    private static readonly int[] PennantY = [6, 16, 3, 8, 6, 3, 1];

    public static void Paint(ArtRaster raster, FrontierState state, FrontierLayout layout, SceneMotion motion, float time)
    {
        if (layout.Narrow)
        {
            return;
        }

        for (int index = 0; index < FrontierLayout.Slots; index++)
        {
            BuildingKind kind = (BuildingKind)index;
            if (!Economy.Unlocked(state, kind))
            {
                continue;
            }

            int cx = layout.SlotCenter(index);
            int level = state.Level(kind);
            if (level == 0)
            {
                ArcanePainters.Plot(raster, cx);
                continue;
            }

            int tier = Economy.Tier(level);
            if (motion.Build[index] is float building)
            {
                int top = (int)Math.Round(FrontierLayout.Ground - FrontierLayout.Ground * Ease(building / SceneMotion.BuildSeconds));
                raster.ClipAbove = top;
                Draw(raster, kind, cx, tier, time);
                raster.ClipAbove = int.MinValue;
                Scaffold(raster, cx, top);
                continue;
            }

            Draw(raster, kind, cx, tier, time);
            Decorate(raster, index, cx, tier, time);
        }
    }

    public static float Ease(float progress)
    {
        float p = Math.Clamp(progress, 0, 1);
        return p < 0.5f ? 2 * p * p : 1 - MathF.Pow(-2 * p + 2, 2) / 2;
    }

    private static void Draw(ArtRaster raster, BuildingKind kind, int cx, int tier, float time)
    {
        switch (kind)
        {
            case BuildingKind.Farm:
                FarmPainters.Farm(raster, cx, tier, time);
                break;
            case BuildingKind.Mill:
                FarmPainters.Mill(raster, cx, tier, time);
                break;
            case BuildingKind.Bakery:
                FarmPainters.Bakery(raster, cx, tier);
                break;
            case BuildingKind.Forge:
                TownPainters.Forge(raster, cx, tier);
                break;
            case BuildingKind.Market:
                TownPainters.Market(raster, cx, tier);
                break;
            case BuildingKind.Tower:
                ArcanePainters.Tower(raster, cx, tier);
                break;
            default:
                ArcanePainters.Portal(raster, cx, tier);
                break;
        }
    }

    private static void Decorate(ArtRaster raster, int index, int cx, int tier, float time)
    {
        if (tier >= 1)
        {
            int x = cx + PennantX[index];
            int y = PennantY[index];
            int wave = (int)(time * 4 + index) % 2;
            raster.Fill(x, y, 1, 4, Palette.Bark);
            raster.Fill(x + 1, y + wave, 3, 2, Palette.Red);
            raster.Set(x + 4, y + 1, Palette.Red);
        }

        if (tier >= 3)
        {
            raster.Fill(cx - 12, 23, 25, 1, Palette.Gray);
            for (int x = cx - 11; x < cx + 12; x += 4)
            {
                raster.Set(x, 23, Palette.Steel);
            }
        }

        if (tier >= 4)
        {
            raster.Set(cx - 13, 22, Palette.Rose);
            raster.Set(cx - 12, 22, Palette.Yellow);
            raster.Set(cx + 12, 22, Palette.Rose);
            raster.Set(cx + 13, 22, Palette.Yellow);
            raster.Fill(cx - 13, 23, 2, 1, Palette.Leaf);
            raster.Fill(cx + 12, 23, 2, 1, Palette.Leaf);
        }
    }

    private static void Scaffold(ArtRaster raster, int cx, int top)
    {
        raster.Fill(cx - 15, top, 1, FrontierLayout.Ground - top, Palette.Bark);
        raster.Fill(cx + 14, top, 1, FrontierLayout.Ground - top, Palette.Bark);
        raster.Fill(cx - 15, top, 30, 1, Palette.Wood);
    }
}
