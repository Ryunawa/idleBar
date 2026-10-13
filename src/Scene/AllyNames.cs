using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public static class AllyNames
{
    private const int MinLetters = 3;
    private const int Lift = 8;

    public static void Paint(ArtRaster r, IReadOnlyList<Post> posts)
    {
        List<Post> allies = [.. posts.Where(post => post.Member.Ally is not null)];
        for (int index = 0; index < allies.Count; index++)
        {
            Ally ally = allies[index].Member.Ally!;
            int center = Center(allies[index]);
            int room = int.MaxValue;
            if (index > 0)
            {
                room = Math.Min(room, center - Center(allies[index - 1]) - 2);
            }

            if (index < allies.Count - 1)
            {
                room = Math.Min(room, Center(allies[index + 1]) - center - 2);
            }

            string name = Fit(ally.Look.Name, room);
            HeroFigure figure = HeroSprites.Of(ally.Role, ally.Look);
            int top = ally.Downed > 0
                ? FrontierLayout.Ground - figure.Down.Height
                : FrontierLayout.Ground - figure.Standing.Height + figure.Top;
            ArtFont.Draw(r, name, center - ArtFont.Width(name) / 2, Math.Max(0, top - Lift), Palette.White, Palette.Ink);
        }
    }

    public static string Fit(string name, int room)
    {
        string fitted = name.Trim();
        while (fitted.Length > MinLetters && ArtFont.Width(fitted) > room)
        {
            fitted = fitted[..^1].TrimEnd();
        }

        return fitted;
    }

    private static int Center(Post post) => post.X + 5;
}
