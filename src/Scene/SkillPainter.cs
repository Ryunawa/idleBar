using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class SkillPainter
{
    private const int Gap = 2;

    public static int ButtonX(FrontierLayout layout, int index) => layout.SkillsX + 3 + index * (layout.SkillSize + Gap);

    public static int ButtonY(FrontierLayout layout) => (FrontierLayout.Rows - layout.SkillSize) / 2;

    public static void Paint(ArtRaster r, FrontierState state, FrontierLayout layout, int hovered, float time)
    {
        r.Fill(layout.SkillsX, -layout.Top, layout.SkillsWidth, FrontierLayout.Rows + layout.Top, Palette.Navy);
        r.Fill(layout.SkillsX, -layout.Top, 1, FrontierLayout.Rows + layout.Top, Palette.Ink);
        int y = ButtonY(layout);
        for (int slot = 0; slot < FrontierState.SlotCount; slot++)
        {
            int x = ButtonX(layout, slot);
            if (state.Slots[slot] is SkillKind kind)
            {
                Button(r, state, kind, SkillCasts.Wait(state, kind), x, y, layout.SkillSize, hovered == slot, time);
            }
            else
            {
                Empty(r, x, y, layout.SkillSize, hovered == slot);
            }
        }
    }

    private static void Button(ArtRaster r, FrontierState state, SkillKind kind, double cooldown, int x, int y, int size, bool hover, float time)
    {
        bool ready = cooldown <= 0;
        bool glow = ready && time % 3 < 0.35f;
        r.Fill(x, y, size, size, glow ? Palette.Gold : Palette.Ink);
        r.Fill(x + 1, y + 1, size - 2, size - 2, hover ? Palette.Steel : Palette.Slate);
        r.Fill(x + 1, y + 1, size - 2, 1, hover ? Palette.Gray : Palette.Steel);
        r.Fill(x + 1, y + size - 2, size - 2, 1, Palette.Navy);
        ArtSprite icon = SceneSprites.SkillIcons[kind];
        r.Blit(icon, x + (size - icon.Width) / 2, y + (size - icon.Height) / 2);
        if (!ready)
        {
            int shade = (int)Math.Round((size - 2) * Math.Min(1, cooldown / Economy.SkillCooldown(state, kind)));
            r.Fill(x + 1, y + 1, size - 2, shade, Palette.Ink.WithAlpha(0.72f));
            string label = cooldown >= 60 ? $"{Math.Ceiling(cooldown / 60)}M" : $"{Math.Ceiling(cooldown)}";
            ArtFont.Draw(r, label, x + (size - ArtFont.Width(label)) / 2, y + (size - ArtFont.Height) / 2, Palette.White, Palette.Ink);
        }

        double left = SkillCasts.Left(state, kind);
        if (left > 0)
        {
            r.Fill(x, y + size, (int)Math.Round(size * Math.Min(1, left / SkillCasts.Duration(state, kind))), 1, Effect(kind));
        }
    }

    private static Rgba Effect(SkillKind kind) => kind switch
    {
        SkillKind.Fury => Palette.Red,
        SkillKind.Storm => Palette.Yellow,
        SkillKind.HolyShield => Palette.White,
        _ => Palette.Gold,
    };

    private static void Empty(ArtRaster r, int x, int y, int size, bool hover)
    {
        r.Fill(x, y, size, size, Palette.Ink);
        r.Fill(x + 1, y + 1, size - 2, size - 2, hover ? Palette.Slate : Palette.Navy.Mix(Palette.Slate, 0.35f));
        int middle = size / 2;
        Rgba mark = hover ? Palette.Gray : Palette.Slate;
        r.Fill(x + middle - 2, y + middle, 5, 1, mark);
        r.Fill(x + middle, y + middle - 2, 1, 5, mark);
    }
}
