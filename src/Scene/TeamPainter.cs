using System;
using System.Collections.Generic;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class TeamPainter
{
    private const int BarWidth = 8;

    public static void Paint(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene)
    {
        SceneMotion motion = scene.Motion;
        IReadOnlyList<Post> posts = Formation.Posts(state, layout);
        foreach (Post post in posts)
        {
            switch (post.Member)
            {
                case { Ally: Ally ally }:
                    Reinforcement(r, state, ally, post.X, scene);
                    break;
                case { Kind: FighterKind.Hero }:
                    Hero(r, state, post.X, scene);
                    break;
                case { Kind: FighterKind kind }:
                    Companion(r, state, kind, FrontPainter.Walk(post.X, layout, kind, motion), motion);
                    break;
            }
        }

        AllyNames.Paint(r, posts);
    }

    public static int HeroLean(HeroRole role, SceneMotion motion) =>
        !Roles.Of(role).Ranged && motion.HeroSwing > 0.4f ? 1 : 0;

    private static void Reinforcement(ArtRaster r, FrontierState state, Ally ally, int x, FrontierScene scene)
    {
        SceneMotion motion = scene.Motion;
        HeroFigure figure = HeroSprites.Of(ally.Role, ally.Look);
        if (ally.Downed > 0)
        {
            r.Blit(figure.Down, x - 2, FrontierLayout.Ground - figure.Down.Height);
            double total = Fighters.Of(FighterKind.Hero).Revive * (Team.SupportUp(state) ? Balance.SupportRevive : 1);
            Bar(r, x + 1, FrontierLayout.Ground - figure.Down.Height - 2, 1 - ally.Downed / total, Palette.Cyan);
            return;
        }

        float swing = motion.AllySwing.GetValueOrDefault(ally.Id);
        int left = x + (!Roles.Of(ally.Role).Ranged && swing > 0.4f ? 1 : 0);
        int y = FrontierLayout.Ground - figure.Standing.Height;
        r.Blit(motion.AllyHurt.ContainsKey(ally.Id) ? figure.Flash : figure.Standing, left, y);
        if (ally.Role == HeroRole.Warrior)
        {
            Sword(r, left + HeroGrids.HandX, y + HeroGrids.HandY, FrontPainter.SwordAngle(swing, scene.Time), Palette.Silver);
        }

        if (ally.Health < 0.999)
        {
            Bar(r, left + 1, y + figure.Top - 2, ally.Health, ally.Health > 0.5 ? Palette.Green : ally.Health > 0.25 ? Palette.Gold : Palette.Red);
        }
    }

    private static void Companion(ArtRaster r, FrontierState state, FighterKind kind, int x, SceneMotion motion)
    {
        int y = FrontierLayout.Ground - (kind == FighterKind.Mage ? SceneSprites.Mage : SceneSprites.Archer).Height;
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

    private static void Hero(ArtRaster r, FrontierState state, int post, FrontierScene scene)
    {
        SceneMotion motion = scene.Motion;
        HeroFigure figure = HeroSprites.Of(state.Role, state.Hero, state.Worn);
        int x = post + HeroLean(state.Role, motion);
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
            Sword(r, x + HeroGrids.HandX, y + HeroGrids.HandY, FrontPainter.SwordAngle(motion, scene.Time), Blade(state.Worn.Weapon));
        }

        Health(r, state, FighterKind.Hero, x + 1, y + figure.Top - 2);
    }

    private static Rgba Blade(int? rarity) => rarity is int rank ? ItemSprites.Rarities[System.Math.Clamp(rank, 0, 3)].Main : Palette.Silver;

    private static void Sword(ArtRaster r, int handX, int handY, float angle, Rgba blade)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        for (int radius = 0; radius <= 9; radius++)
        {
            int px = (int)MathF.Round(handX + cos * radius);
            int py = (int)MathF.Round(handY + sin * radius);
            r.Set(px, py, radius <= 1 ? Palette.Bark : radius == 2 ? Palette.Gold : radius == 9 ? Palette.White : blade);
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
