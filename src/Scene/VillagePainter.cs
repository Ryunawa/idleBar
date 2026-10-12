using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class VillagePainter
{
    private const int CartWidth = 18;

    public static void Paint(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene)
    {
        float time = scene.Time;
        if (layout.Palisade)
        {
            Palisade(r, layout.PalisadeX, time);
        }

        foreach (Villager villager in scene.Village.Villagers)
        {
            Walker(r, villager, time);
        }

        if (layout.Crier)
        {
            Crier(r, layout.CrierX, scene.Village.Crier.Showing > 0, time);
        }

        if (layout.CartTo - layout.CartFrom < 30)
        {
            return;
        }

        foreach (Convoy convoy in state.Convoys)
        {
            bool outward = convoy.Progress < 1;
            Cart(r, CartLeft(layout, convoy), outward, (int)(time * 7 + convoy.Id) % 2 == 0);
        }
    }

    public static int CartLeft(FrontierLayout layout, Convoy convoy)
    {
        bool outward = convoy.Progress < 1;
        double leg = BuildingPainter.Ease((float)(outward ? convoy.Progress : convoy.Progress - 1));
        double x = outward ? layout.CartFrom + (layout.CartTo - layout.CartFrom) * leg : layout.CartTo - (layout.CartTo - layout.CartFrom) * leg;
        return (int)Math.Round(x);
    }

    private static void Palisade(ArtRaster r, int x0, float time)
    {
        int[] heights = [12, 13, 12];
        for (int stake = 0; stake < heights.Length; stake++)
        {
            int x = x0 + stake * 3;
            int h = heights[stake];
            r.Fill(x, FrontierLayout.Ground - h, 2, h, Palette.Wood);
            r.Fill(x + 1, FrontierLayout.Ground - h, 1, h, Palette.Bark);
            r.Set(x, FrontierLayout.Ground - 1 - h, Palette.Wood);
        }

        r.Fill(x0 - 1, 17, 10, 1, Palette.Bark);
        int pole = x0 + 10;
        r.Fill(pole, 5, 1, 19, Palette.Bark);
        bool wave = (int)(time * 4) % 2 == 0;
        r.Fill(pole + 1, 5, 5, 3, Palette.Red);
        r.Set(pole + 6, wave ? 5 : 6, Palette.Red);
        r.Set(pole + 6, 7, wave ? Palette.Red : Palette.Wine);
        r.Set(pole + 3, 6, Palette.Yellow);
    }

    private static void Walker(ArtRaster r, Villager villager, float time)
    {
        int x = (int)Math.Round(villager.X);
        bool step = villager.Pause <= 0 && (int)(time * 6 + villager.Phase) % 2 == 1;
        r.Fill(x, 18, 3, 1, villager.Hair);
        r.Fill(x, 19, 3, 1, Palette.Skin);
        r.Fill(x, 20, 3, 2, villager.Shirt);
        if (step)
        {
            r.Fill(x + 1, 22, 1, 2, Palette.Soil);
            return;
        }

        r.Fill(x, 22, 1, 2, Palette.Soil);
        r.Fill(x + 2, 22, 1, 2, Palette.Soil);
    }

    private static void Crier(ArtRaster r, int x0, bool speaking, float time)
    {
        int x = x0 + 1;
        r.Fill(x - 1, 21, 7, 3, Palette.Wood);
        r.Fill(x - 1, 21, 7, 1, Palette.Peach);
        r.Fill(x + 2, 22, 1, 2, Palette.Bark);
        r.Fill(x + 1, 13, 3, 1, Palette.Red);
        r.Fill(x, 14, 5, 1, Palette.Wine);
        r.Fill(x + 1, 15, 3, 2, Palette.Skin);
        r.Fill(x + 1, 17, 3, 2, Palette.NavyBlue);
        r.Fill(x + 1, 19, 1, 2, Palette.Soil);
        r.Fill(x + 3, 19, 1, 2, Palette.Soil);
        if (!speaking)
        {
            r.Set(x + 4, 18, Palette.NavyBlue);
            return;
        }

        int wiggle = (int)(time * 8) % 2;
        r.Set(x + 4, 16, Palette.Skin);
        r.Set(x + 5, 15 - wiggle, Palette.Gold);
        r.Set(x + 5, 16 - wiggle, Palette.Gold);
    }

    private static void Cart(ArtRaster r, int x, bool outward, bool step)
    {
        Part(r, x, outward, 0, 18, 9, 4, Palette.Wood);
        Part(r, x, outward, 0, 18, 9, 1, Palette.Peach);
        Part(r, x, outward, 0, 20, 9, 1, Palette.Bark);
        Part(r, x, outward, 1, 22, 2, 2, Palette.Soil);
        Part(r, x, outward, 6, 22, 2, 2, Palette.Soil);
        if (outward)
        {
            Part(r, x, outward, 1, 16, 3, 2, Palette.Sand);
            Part(r, x, outward, 4, 15, 3, 3, Palette.Sand);
            Part(r, x, outward, 7, 16, 2, 2, Palette.Peach);
            Part(r, x, outward, 5, 15, 1, 1, Palette.White);
        }

        Part(r, x, outward, 9, 19, 2, 1, Palette.Bark);
        Part(r, x, outward, 11, 18, 5, 3, Palette.Gray);
        Part(r, x, outward, 11, 20, 5, 1, Palette.Steel);
        Part(r, x, outward, 10, 18, 1, 2, Palette.Steel);
        Part(r, x, outward, 15, 16, 3, 3, Palette.Gray);
        Part(r, x, outward, 15, 14, 1, 2, Palette.Gray);
        Part(r, x, outward, 16, 15, 1, 1, Palette.Steel);
        Part(r, x, outward, 17, 18, 1, 1, Palette.Steel);
        Part(r, x, outward, 16, 17, 1, 1, Palette.Ink);
        Part(r, x, outward, step ? 11 : 12, 21, 1, 3, Palette.Steel);
        Part(r, x, outward, step ? 15 : 14, 21, 1, 3, Palette.Steel);
    }

    private static void Part(ArtRaster r, int x, bool facingRight, int left, int y, int width, int height, Rgba color) =>
        r.Fill(facingRight ? x + left : x + CartWidth - left - width, y, width, height, color);
}
