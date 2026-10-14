using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class BossButton
{
    private const int SkyTop = 2;
    private const int Height = 9;
    private const int Padding = 3;

    public static bool Contains(FrontierState state, FrontierLayout layout, int x, int y) =>
        state.BossHeld && x >= Left(state, layout) && x < Left(state, layout) + Width(state) && y >= Top(state, layout) && y < Top(state, layout) + Height;

    public static void Paint(ArtRaster r, FrontierState state, FrontierLayout layout, float time)
    {
        if (!state.BossHeld)
        {
            return;
        }

        int x = Left(state, layout);
        int width = Width(state);
        int top = Top(state, layout);
        bool pulse = time % 1.2f < 0.6f;
        r.Fill(x, top, width, Height, Palette.Ink);
        r.Fill(x + 1, top + 1, width - 2, Height - 2, pulse ? Palette.Red : Palette.Wine);
        r.Fill(x + 1, top + 1, width - 2, 1, pulse ? Palette.Orange : Palette.Red);
        ArtFont.Draw(r, Label(state), x + Padding, top + (Height - ArtFont.Height) / 2, Palette.White, Palette.Ink);
    }

    private static string Label(FrontierState state) => $"BOSS {(state.Stage / Balance.BossEvery + 1) * Balance.BossEvery}";

    private static int Width(FrontierState state) => ArtFont.Width(Label(state)) + Padding * 2;

    private static bool InFog(FrontierState state, FrontierLayout layout) => layout.FogWidth >= Width(state) + 4;

    private static int Top(FrontierState state, FrontierLayout layout) => InFog(state, layout) ? FrontierLayout.Rows - Height - 1 : SkyTop;

    private static int Left(FrontierState state, FrontierLayout layout)
    {
        int width = Width(state);
        return InFog(state, layout) ? layout.FogX + (layout.FogWidth - width) / 2 : layout.FogX - width - 2;
    }
}
