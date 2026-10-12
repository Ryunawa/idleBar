using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class BossButton
{
    private const int Top = 2;
    private const int Height = 11;
    private const int Padding = 3;

    public static bool Contains(FrontierState state, FrontierLayout layout, int x, int y) =>
        state.BossHeld && x >= Left(state, layout) && x < Left(state, layout) + Width(state) && y >= Top && y < Top + Height;

    public static void Paint(ArtRaster r, FrontierState state, FrontierLayout layout, float time)
    {
        if (!state.BossHeld)
        {
            return;
        }

        int x = Left(state, layout);
        int width = Width(state);
        bool pulse = time % 1.2f < 0.6f;
        r.Fill(x, Top, width, Height, Palette.Ink);
        r.Fill(x + 1, Top + 1, width - 2, Height - 2, pulse ? Palette.Red : Palette.Wine);
        r.Fill(x + 1, Top + 1, width - 2, 1, pulse ? Palette.Orange : Palette.Red);
        ArtFont.Draw(r, Label(state), x + Padding, Top + (Height - ArtFont.Height) / 2, Palette.White, Palette.Ink);
    }

    private static string Label(FrontierState state) => $"BOSS {state.Stage + 1}";

    private static int Width(FrontierState state) => ArtFont.Width(Label(state)) + Padding * 2;

    private static int Left(FrontierState state, FrontierLayout layout)
    {
        int width = Width(state);
        return layout.FogWidth >= width + 4 ? layout.FogX + (layout.FogWidth - width) / 2 : layout.FogX - width - 2;
    }
}
