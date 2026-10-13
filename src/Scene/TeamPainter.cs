using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class TeamPainter
{
    private const int BarWidth = 8;
    private const int ArcherY = 12;
    private const int MageY = 11;

    public static void Paint(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene)
    {
        SceneMotion motion = scene.Motion;
        Companion(r, state, FighterKind.Mage, FrontPainter.Walk(layout, FighterKind.Mage, motion), MageY, motion);
        Companion(r, state, FighterKind.Archer, FrontPainter.Walk(layout, FighterKind.Archer, motion), ArcherY, motion);
        Hero(r, state, layout, scene);
    }

    public static int HeroLean(HeroRole role, SceneMotion motion) =>
        !Roles.Of(role).Ranged && motion.HeroSwing > 0.4f ? 1 : 0;

    private static void Companion(ArtRaster r, FrontierState state, FighterKind kind, int x, int y, SceneMotion motion)
    {
        if (state.Level(kind) <= 0)
        {
            return;
        }

        bool archer = kind == FighterKind.Archer;
        if (!Vitality.Up(state, kind))
        {
            ArtSprite down = archer ? SceneSprites.ArcherDown : SceneSprites.MageDown;
            r.Blit(down, x - 2, FrontierLayout.Ground - down.Height);
            Revival(r, state, kind, x, FrontierLayout.Ground - down.Height - 2);
            return;
        }

        bool hurt = motion.Hurt[(int)kind] > 0;
        r.Blit(archer ? hurt ? SceneSprites.ArcherFlash : SceneSprites.Archer : hurt ? SceneSprites.MageFlash : SceneSprites.Mage, x, y);
        Health(r, state, kind, x, y - 2);
    }

    private static void Hero(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene)
    {
        SceneMotion motion = scene.Motion;
        HeroFigure figure = HeroSprites.Of(state.Role, state.Hero);
        int x = layout.HeroX + HeroLean(state.Role, motion);
        int y = FrontierLayout.Ground - figure.Standing.Height;
        if (!Vitality.Up(state, FighterKind.Hero))
        {
            r.Blit(figure.Down, x - 2, FrontierLayout.Ground - figure.Down.Height);
            Revival(r, state, FighterKind.Hero, x + 1, FrontierLayout.Ground - figure.Down.Height - 2);
            return;
        }

        if (state.FuryLeft > 0 && (int)(scene.Time * 10) % 2 == 0)
        {
            r.Blit(figure.Rage, x - 1, y);
            r.Blit(figure.Rage, x + 1, y);
            r.Blit(figure.Rage, x, y - 1);
        }

        r.Blit(motion.Hurt[(int)FighterKind.Hero] > 0 ? figure.Flash : figure.Standing, x, y);
        if (state.Role == HeroRole.Warrior)
        {
            Sword(r, x + HeroGrids.HandX, y + HeroGrids.HandY, FrontPainter.SwordAngle(motion, scene.Time));
        }

        Health(r, state, FighterKind.Hero, x + 1, y + figure.Top - 2);
    }

    private static void Sword(ArtRaster r, int handX, int handY, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        for (int radius = 0; radius <= 9; radius++)
        {
            int px = (int)MathF.Round(handX + cos * radius);
            int py = (int)MathF.Round(handY + sin * radius);
            r.Set(px, py, radius <= 1 ? Palette.Bark : radius == 2 ? Palette.Gold : radius == 9 ? Palette.White : Palette.Silver);
            if (radius == 2)
            {
                r.Set((int)MathF.Round(px - sin), (int)MathF.Round(py + cos), Palette.Gold);
                r.Set((int)MathF.Round(px + sin), (int)MathF.Round(py - cos), Palette.Gold);
            }
        }
    }

    private static void Health(ArtRaster r, FrontierState state, FighterKind kind, int x, int y)
    {
        double health = state.Health[(int)kind];
        if (health < 0.999)
        {
            Bar(r, x, y, health, health > 0.5 ? Palette.Green : health > 0.25 ? Palette.Gold : Palette.Red);
        }
    }

    private static void Revival(ArtRaster r, FrontierState state, FighterKind kind, int x, int y)
    {
        double total = Math.Max(1, Vitality.ReviveSeconds(state, kind));
        Bar(r, x, y, 1 - state.Downed[(int)kind] / total, Palette.Cyan);
    }

    private static void Bar(ArtRaster r, int x, int y, double share, Rgba fill)
    {
        r.Fill(x, y, BarWidth, 1, Palette.Ink);
        r.Fill(x, y, Math.Clamp((int)Math.Round(BarWidth * share), 1, BarWidth), 1, fill);
    }
}
