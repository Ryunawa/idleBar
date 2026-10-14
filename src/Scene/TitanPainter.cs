using System;
using System.Collections.Concurrent;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class TitanPainter
{
    public const float FallSeconds = 2.5f;

    private const int Scale = 2;
    private const int MinFog = 34;
    private const int Top = 9;
    private const int GaugeY = 1;
    private const int BarY = 7;
    private const int Horizon = FrontierLayout.Ground - 1;
    private const int Mist = 9;
    private const float Opacity = 0.8f;

    private static readonly Rgba[] Bodies = [Palette.Steel, Palette.Leaf, Palette.Blue, Palette.Rust];
    private static readonly ConcurrentDictionary<int, (ArtSprite Body, ArtSprite Flash)> Cache = new();

    public static ArtPoint? Place(FrontierState state, FrontierLayout layout)
    {
        if (state.Titan is not TitanView titan || layout.FogWidth < MinFog)
        {
            return null;
        }

        ArtSprite body = Sprites(titan.Kind).Body;
        return new ArtPoint(layout.FogX + (layout.FogWidth - body.Width) / 2, Top);
    }

    public static bool Contains(FrontierState state, FrontierLayout layout, int x, int y) =>
        Place(state, layout) is ArtPoint place && state.Titan is { Fallen: false } titan
        && x >= place.X && x < place.X + Sprites(titan.Kind).Body.Width && y >= place.Y;

    public static void Paint(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene)
    {
        if (Place(state, layout) is not ArtPoint place || state.Titan is not TitanView titan)
        {
            return;
        }

        float? fall = scene.Motion.TitanFall;
        if (titan.Fallen && fall is null)
        {
            return;
        }

        (ArtSprite body, ArtSprite flash) = Sprites(titan.Kind);
        int sink = fall is float time ? (int)(time / FallSeconds * 34) : 0;
        int breath = MathF.Sin(scene.Time * 1.3f) > 0.7f ? 1 : 0;
        Rise(r, scene.Motion.TitanFlash > 0 ? flash : body, place.X, place.Y + breath + sink);
    }

    private static void Rise(ArtRaster r, ArtSprite sprite, int x, int y)
    {
        for (int row = 0; row < sprite.Height && y + row < Horizon; row++)
        {
            float alpha = Opacity * Math.Clamp((Horizon - y - row) / (float)Mist, 0.15f, 1);
            for (int column = 0; column < sprite.Width; column++)
            {
                if (sprite[column, row] is Rgba color)
                {
                    r.Set(x + column, y + row, color.WithAlpha(alpha));
                }
            }
        }
    }

    public static void Gauge(ArtRaster r, FrontierState state, FrontierLayout layout)
    {
        if (state.Titan is not TitanView titan || layout.FogWidth < MinFog)
        {
            return;
        }

        int x = layout.FogX + 3;
        int width = layout.FogWidth - 6;
        string label = titan.Fallen ? "TITAN ABATTU" : $"TITAN {Math.Ceiling(titan.Left * 100)}%";
        ArtFont.Draw(r, label, x, GaugeY, titan.Fallen ? Palette.Gold : Palette.White, Palette.Ink);
        if (!titan.Fallen)
        {
            r.Fill(x, BarY, width, 2, Palette.Ink);
            r.Fill(x, BarY, Math.Max(1, (int)Math.Round(width * titan.Left)), 2, Palette.Red);
        }
    }

    public static (ArtSprite Body, ArtSprite Flash) Sprites(int kind) => Cache.GetOrAdd(((kind % 4) + 4) % 4, Build);

    private static (ArtSprite Body, ArtSprite Flash) Build(int kind)
    {
        Rgba body = Bodies[kind];
        Rgba shade = body.Mix(Palette.Ink, 0.45f);
        ArtSprite boss = SceneSprites.Bosses[kind is 1 or 2 ? 1 : 0];
        ArtSprite sprite = boss.Map(color => Glows(color) ? Palette.Red : Light(color) ? body : shade).Scaled(Scale);
        return (sprite, sprite.Silhouette(Palette.White));
    }

    private static bool Glows(Rgba color) => color == Palette.Yellow || color == Palette.White;

    private static bool Light(Rgba color) => color.R * 0.3 + color.G * 0.59 + color.B * 0.11 > 110;
}
