using System;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class OverlayPainter
{
    public static void Monster(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene)
    {
        if (state.Monster is not Monster monster || scene.Box(state, layout) is not MonsterBox box)
        {
            return;
        }

        int width = monster.Boss ? 28 : 20;
        int x = box.CenterX - width / 2;
        int y = Math.Max(1, box.Y - 4);
        r.Fill(x - 1, y - 1, width + 2, 4, monster.Boss ? Palette.Gold : Palette.Ink);
        r.Fill(x, y, width, 2, Palette.Soil);
        int fill = (int)Math.Max(0, Math.Round(width * monster.Hp / monster.MaxHp));
        r.Fill(x, y, fill, 2, Palette.Red);
        r.Fill(x, y, fill, 1, Palette.Rose);
        if (monster.Boss && monster.Approach <= 0)
        {
            r.Fill(x, y + 3, (int)Math.Round(width * monster.BossLeft / Balance.BossSeconds), 1, Palette.Yellow);
        }

        if (state.Supply > 0)
        {
            r.Blit(SceneSprites.Bread, layout.HeroX, 4);
            for (int stack = 0; stack < state.Supply; stack++)
            {
                r.Set(layout.HeroX + 6 + stack * 2, 5, Palette.Yellow);
            }
        }
    }

    public static void Chicken(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene)
    {
        if (state.Chicken is double flight)
        {
            ArtPoint at = scene.Chicken(state, layout);
            r.Blit(SceneSprites.Chicken[(int)(flight * 8) % 2], at.X, at.Y);
        }
    }

    public static void Locks(ArtRaster r, FrontierState state, FrontierLayout layout, SkyTones sky)
    {
        if (layout.Narrow)
        {
            return;
        }

        Rgba ink = sky.FogDark.Mix(Palette.Ink, 0.75f);
        foreach (BuildingInfo building in Buildings.All.Where(building => !Economy.Unlocked(state, building.Kind)))
        {
            int cx = layout.SlotCenter(building.Kind);
            string label = $"BOSS {building.Unlock - 1}";
            r.Blit(SceneSprites.Lock, cx - 2, 6);
            ArtFont.Draw(r, label, cx - ArtFont.Width(label) / 2, 15, ink);
        }
    }

    public static void Bubble(ArtRaster r, FrontierLayout layout, Crier crier)
    {
        if (!layout.Crier || crier.Showing <= 0)
        {
            return;
        }

        int width = ArtFont.Width(crier.Text) + 6;
        int left = Math.Max(layout.VillageX + 2, layout.CrierX + 12 - width);
        r.Fill(left - 1, 1, width + 2, 9, Palette.Ink);
        r.Fill(left, 0, width, 11, Palette.Ink);
        r.Fill(left, 1, width, 9, Palette.White);
        int tail = layout.CrierX + 3;
        r.Set(tail, 10, Palette.White);
        r.Set(tail - 1, 11, Palette.Ink);
        r.Set(tail + 1, 11, Palette.Ink);
        r.Set(tail, 11, Palette.White);
        r.Set(tail, 12, Palette.Ink);
        ArtFont.Draw(r, crier.Text, left + 3, 3, Palette.Ink);
    }

    public static void Floats(ArtRaster r, FrontierLayout layout, SceneFx fx)
    {
        foreach (FloatText text in fx.Floats)
        {
            int width = ArtFont.Width(text.Text);
            int x = Math.Clamp((int)MathF.Round(text.X - width / 2f), 1, Math.Max(1, layout.SkillsX - width - 2));
            float alpha = Math.Min(1, text.Life / 0.25f);
            ArtFont.Draw(r, text.Text, x, (int)MathF.Round(text.Y), text.Color.WithAlpha(alpha), Palette.Ink.WithAlpha(alpha));
        }
    }

    public static void Banner(ArtRaster r, FrontierLayout layout, BannerQueue banners, SceneMotion motion, float time)
    {
        if (layout.Narrow)
        {
            return;
        }

        int center = (layout.Narrow ? layout.FogX : layout.VillageEnd) / 2 + 2;
        if (motion.Replay > 0 && (int)(time * 4) % 2 == 1)
        {
            ArtFont.Draw(r, motion.ReplayLabel, center - ArtFont.Width(motion.ReplayLabel) / 2, 2, Palette.Yellow, Palette.Ink);
        }

        if (banners.Current is not Banner banner)
        {
            return;
        }

        int width = banner.Lines.Max(ArtFont.Width) + 12;
        int height = banner.Lines.Count * 7 + 3;
        int x = Math.Max(1, center - width / 2);
        int y = (FrontierLayout.Ground - height) / 2 - (banner.Time < 0.2f ? (int)MathF.Round((0.2f - banner.Time) * 40) : 0);
        r.Fill(x - 1, y - 1, width + 2, height + 2, Palette.Ink);
        r.Fill(x, y, width, height, Palette.Navy);
        r.Fill(x, y, width, 1, Palette.Gold);
        r.Fill(x, y + height - 1, width, 1, Palette.Gold);
        for (int line = 0; line < banner.Lines.Count; line++)
        {
            string text = banner.Lines[line];
            ArtFont.Draw(r, text, x + (width - ArtFont.Width(text)) / 2, y + 2 + line * 7, line == 0 ? Palette.Yellow : Palette.White);
        }
    }
}
