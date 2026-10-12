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
        int size = layout.SkillSize;
        int y = ButtonY(layout);
        foreach (SkillInfo skill in Skills.All)
        {
            int index = (int)skill.Kind;
            int x = ButtonX(layout, index);
            if (!Skills.Unlocked(state, skill.Kind))
            {
                Locked(r, x, y, size, skill.Unlock, hovered == index);
                continue;
            }

            double cooldown = state.Cooldowns[index];
            bool ready = cooldown <= 0;
            bool hover = hovered == index;
            bool glow = ready && time % 3 < 0.35f;
            r.Fill(x, y, size, size, glow ? Palette.Gold : Palette.Ink);
            r.Fill(x + 1, y + 1, size - 2, size - 2, hover ? Palette.Steel : Palette.Slate);
            r.Fill(x + 1, y + 1, size - 2, 1, hover ? Palette.Gray : Palette.Steel);
            r.Fill(x + 1, y + size - 2, size - 2, 1, Palette.Navy);
            ArtSprite icon = SceneSprites.Skills[index];
            r.Blit(icon, x + (size - icon.Width) / 2, y + (size - icon.Height) / 2);
            if (!ready)
            {
                int shade = (int)Math.Round((size - 2) * Math.Min(1, cooldown / Economy.SkillCooldown(state, skill.Kind)));
                r.Fill(x + 1, y + 1, size - 2, shade, Palette.Ink.WithAlpha(0.72f));
                string label = cooldown >= 60 ? $"{Math.Ceiling(cooldown / 60)}M" : $"{Math.Ceiling(cooldown)}";
                ArtFont.Draw(r, label, x + (size - ArtFont.Width(label)) / 2, y + (size - ArtFont.Height) / 2, Palette.White, Palette.Ink);
            }

            if (skill.Kind == SkillKind.Fury && state.FuryLeft > 0)
            {
                r.Fill(x, y + size, (int)Math.Round(size * state.FuryLeft / skill.Duration), 1, Palette.Red);
            }
        }
    }

    private static void Locked(ArtRaster r, int x, int y, int size, int unlock, bool hover)
    {
        r.Fill(x, y, size, size, Palette.Ink);
        r.Fill(x + 1, y + 1, size - 2, size - 2, hover ? Palette.Slate : Palette.Navy.Mix(Palette.Slate, 0.35f));
        ArtSprite icon = SceneSprites.Lock;
        r.Blit(icon, x + (size - icon.Width) / 2, y + 3);
        string label = $"{unlock}";
        ArtFont.Draw(r, label, x + (size - ArtFont.Width(label)) / 2, y + size - ArtFont.Height - 3, Palette.Gray);
    }
}
