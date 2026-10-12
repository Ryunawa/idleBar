using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class FrontPainter
{
    private static readonly Rgba[][] SlimeColors =
    [
        [Palette.Green, Palette.Leaf, Palette.Pine],
        [Palette.Cyan, Palette.Blue, Palette.NavyBlue],
        [Palette.Rose, Palette.Pink, Palette.Plum],
    ];

    public static void Paint(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene)
    {
        if (layout.FogWidth >= 30)
        {
            r.Blit(SceneSprites.Shadows[Biomes.IndexOf(state.Stage) * 2 % SceneSprites.Shadows.Count], layout.FogX + 26, 12, alpha: 0.45f);
        }

        SceneMotion motion = scene.Motion;
        if (state.Level(FighterKind.Mage) > 0)
        {
            r.Blit(SceneSprites.Mage, Walk(layout, FighterKind.Mage, motion), 11);
        }

        if (state.Level(FighterKind.Archer) > 0)
        {
            r.Blit(SceneSprites.Archer, Walk(layout, FighterKind.Archer, motion), 12);
        }

        Hero(r, layout.HeroX + (motion.HeroSwing > 0.4f ? 1 : 0), state.FuryLeft > 0 && (int)(scene.Time * 10) % 2 == 0, motion, scene.Time);
        if (motion.Fleeing is MonsterBox fleeing)
        {
            Monster(r, fleeing with { X = fleeing.X + (int)(motion.FleeTime * 40) }, false, (int)(scene.Time * 8) % 2, scene.Time);
        }

        if (scene.Box(state, layout) is MonsterBox box && state.Monster is Monster monster)
        {
            int bob = monster.Approach > 0 ? (int)(scene.Time * 8) % 2 : 0;
            Monster(r, box, motion.MonsterFlash > 0, bob, scene.Time);
        }

        if (motion.Corpse is MonsterBox corpse && (int)(motion.CorpseTime * 30) % 2 == 0)
        {
            Monster(r, corpse, true, 0, scene.Time);
        }
    }

    public static int Walk(FrontierLayout layout, FighterKind kind, SceneMotion motion)
    {
        int post = layout.FighterX(kind);
        if (motion.Recruit[(int)kind] is not float time)
        {
            return post;
        }

        int gate = layout.Narrow ? layout.FrontX : layout.PalisadeX + 8;
        return (int)Math.Round(gate + (post - gate) * BuildingPainter.Ease(time / SceneMotion.RecruitSeconds));
    }

    public static float SwordAngle(SceneMotion motion, float time) =>
        motion.HeroSwing > 0 ? -2.0f + (1 - motion.HeroSwing) * 2.6f : -1.15f + MathF.Sin(time * 2) * 0.06f;

    private static void Hero(ArtRaster r, int x, bool raging, SceneMotion motion, float time)
    {
        const int y = 10;
        if (raging)
        {
            r.Blit(SceneSprites.HeroRage, x - 1, y);
            r.Blit(SceneSprites.HeroRage, x + 1, y);
            r.Blit(SceneSprites.HeroRage, x, y - 1);
        }

        r.Blit(SceneSprites.Hero, x, y);
        float angle = SwordAngle(motion, time);
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        int handX = x + 8;
        int handY = y + 8;
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

    private static void Monster(ArtRaster r, MonsterBox box, bool white, int bob, float time)
    {
        if (!box.Boss && SceneSprites.Monsters[box.Template] is null)
        {
            Slime(r, box, white, time);
            return;
        }

        if (box.Boss && box.Template == 0 && !white)
        {
            r.Fill(box.X - 2, box.Y + 4, 3, 9, Palette.Bark);
            r.Fill(box.X - 3, box.Y + 2, 5, 3, Palette.Bark);
            r.Set(box.X - 1, box.Y + 3, Palette.Silver);
        }

        ArtSprite sprite = white
            ? box.Boss ? SceneSprites.WhiteBosses[box.Template] : SceneSprites.WhiteMonsters[box.Template]!
            : box.Boss ? SceneSprites.Bosses[box.Template] : SceneSprites.Monsters[box.Template]!;
        r.Blit(sprite, box.X, box.Y - bob);
    }

    private static void Slime(ArtRaster r, MonsterBox box, bool white, float time)
    {
        Rgba[] colors = SlimeColors[Biomes.LoopOf(box.Stage) % SlimeColors.Length];
        int height = 8 + (MathF.Sin(time * 5) > 0 ? 1 : 0);
        int width = 12 - (height - 8);
        int cx = box.X + 6;
        int top = FrontierLayout.Ground - height;
        for (int row = 0; row < height; row++)
        {
            float v = (row + 0.5f) / height;
            int half = Math.Max(1, (int)MathF.Round(width / 2f * MathF.Sqrt(1 - (1 - v) * (1 - v))));
            int y = top + row;
            if (white)
            {
                r.Fill(cx - half, y, half * 2, 1, Palette.White);
                continue;
            }

            r.Fill(cx - half, y, half * 2, 1, row == 0 ? colors[2] : row > height - 3 ? colors[1] : colors[0]);
            r.Set(cx - half, y, colors[2]);
            r.Set(cx + half - 1, y, colors[2]);
        }

        if (!white)
        {
            r.Set(cx - 3, top + 2, Palette.White);
            r.Set(cx - 3, top + 3, Palette.Ink);
            r.Set(cx - 1, top + 3, Palette.Ink);
            r.Set(cx - 2, top + 5, colors[2]);
        }
    }
}
