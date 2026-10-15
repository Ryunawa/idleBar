using System;
using System.Collections.Generic;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class TeamPainter
{
    private const int BarWidth = 8;

    private static readonly HeroLook[] CompanionLooks = [new("Premier", 1, 1, 2, 2), new("Second", 2, 2, 0, 6)];

    public static void Paint(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene)
    {
        foreach (Post post in Formation.Posts(state, layout))
        {
            Stance stance = Of(state, layout, post, scene.Motion);
            Figure(r, state, post, stance, scene.Time);
        }
    }

    public static void Bars(ArtRaster r, FrontierState state, FrontierLayout layout, FrontierScene scene)
    {
        IReadOnlyList<Post> posts = Formation.Posts(state, layout);
        List<(int X, int Top)> breads = [];
        foreach (Post post in posts)
        {
            Stance stance = Of(state, layout, post, scene.Motion);
            HeroFigure figure = stance.Figure;
            if (stance.Down)
            {
                Bar(r, stance.X + 1, FrontierLayout.Ground - figure.Down.Height - 2, stance.Revival, Palette.Cyan);
            }
            else
            {
                int top = FrontierLayout.Ground - figure.Standing.Height + figure.Top;
                if (stance.Health < 0.999)
                {
                    Rgba fill = stance.Health > 0.5 ? Palette.Green : stance.Health > 0.25 ? Palette.Gold : Palette.Red;
                    Bar(r, stance.X + 1, top - 2, stance.Health, fill);
                }

                if (post.Member.Ally is null)
                {
                    breads.Add((stance.X, top));
                    Bar(r, stance.X + 1, FrontierLayout.Ground + 1, Actives.Charge(state, post.Member.Kind), Palette.Yellow);
                }
            }
        }

        AllyNames.Paint(r, posts);
        foreach ((int x, int top) in breads)
        {
            Bread(r, state.Supply, x, top);
        }
    }

    public static int HeroLean(HeroRole role, SceneMotion motion) => Lean(role, motion.HeroSwing);

    public static HeroLook CompanionLook(FighterKind kind) => CompanionLooks[Math.Max(0, (int)kind - 1)];

    private static Stance Of(FrontierState state, FrontierLayout layout, Post post, SceneMotion motion)
    {
        if (post.Member.Ally is Ally ally)
        {
            float swing = motion.AllySwing.GetValueOrDefault(ally.Id);
            double total = Fighters.Of(FighterKind.Hero).Revive;
            return new Stance(ally.Role, HeroSprites.Of(ally.Role, ally.Look), post.X + Lean(ally.Role, swing), swing,
                motion.AllyHurt.ContainsKey(ally.Id), ally.Downed > 0, ally.Health, 1 - ally.Downed / total);
        }

        FighterKind kind = post.Member.Kind;
        HeroRole role = state.ClassOf(kind);
        bool hero = kind == FighterKind.Hero;
        float stroke = hero ? motion.HeroSwing : motion.CompanionSwing[(int)kind];
        HeroFigure figure = HeroSprites.Of(role, hero ? state.Hero : CompanionLook(kind), state.Worn[(int)kind]);
        int x = (hero ? post.X : FrontPainter.Walk(post.X, layout, kind, motion)) + Lean(role, stroke);
        double revival = 1 - state.Downed[(int)kind] / Math.Max(1, Vitality.ReviveSeconds(state, kind));
        return new Stance(role, figure, x, stroke, motion.Hurt[(int)kind] > 0, !Vitality.Up(state, kind), state.Health[(int)kind], revival);
    }

    private static void Figure(ArtRaster r, FrontierState state, Post post, Stance stance, float time)
    {
        HeroFigure figure = stance.Figure;
        if (stance.Down)
        {
            r.Blit(figure.Down, stance.X - 2, FrontierLayout.Ground - figure.Down.Height);
            return;
        }

        int y = FrontierLayout.Ground - figure.Standing.Height;
        bool blink = (int)(time * 10) % 2 == 0;
        ArtSprite? glow = state.ShieldLeft > 0 && (state.ShieldLeft > 1 || blink) ? figure.Aura : state.FuryLeft > 0 && blink ? figure.Rage : null;
        if (glow is not null)
        {
            r.Blit(glow, stance.X - 1, y);
            r.Blit(glow, stance.X + 1, y);
            r.Blit(glow, stance.X, y - 1);
        }

        r.Blit(stance.Hurt ? figure.Flash : figure.Standing, stance.X, y);
        if (stance.Role == HeroRole.Warrior)
        {
            Rgba blade = post.Member.Ally is null ? Blade(state.Worn[(int)post.Member.Kind].Weapon) : Palette.Silver;
            Sword(r, stance.X + HeroGrids.HandX, y + HeroGrids.HandY, FrontPainter.SwordAngle(stance.Swing, time), blade);
        }
    }

    private static int Lean(HeroRole role, float swing) => !Roles.Of(role).Ranged && swing > 0.4f ? 1 : 0;

    private static Rgba Blade(int? rarity) => rarity is int rank ? ItemSprites.Rarities[Math.Clamp(rank, 0, 3)].Main : Palette.Silver;

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

    private static void Bread(ArtRaster r, int stacks, int x, int top)
    {
        if (stacks <= 0)
        {
            return;
        }

        r.Blit(SceneSprites.Bread, x + 3, top - 7);
        for (int stack = 0; stack < stacks; stack++)
        {
            r.Set(x + 1 + stack * 2, top - 4, Palette.Yellow);
        }
    }

    private static void Bar(ArtRaster r, int x, int y, double share, Rgba fill)
    {
        r.Fill(x, y, BarWidth, 1, Palette.Ink);
        r.Fill(x, y, Math.Clamp((int)Math.Round(BarWidth * share), 1, BarWidth), 1, fill);
    }
}
