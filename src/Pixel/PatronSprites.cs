using System.Collections.Generic;
using System.Linq;
using Godot;

namespace IdleBar.Pixel;

public static class PatronSprites
{
    public const int Width = 9;
    public const int Height = 13;
    public const int MouthRow = 5;
    private const int CacheLimit = 256;
    private const char Eye = '3';
    private const char Skin = '2';
    private const char Shade = '4';

    private static readonly Color EyeColor = new("1a1410");

    private static readonly string[][] Heads =
    [
        ["..11111..", ".1111111.", ".1222221.", ".2322232.", ".2222222.", "..22422.."],
        ["..11111..", ".1111111.", "112222211", "123222321", "122222221", "1.22422.1"],
        ["...777...", "..77777..", ".7722277.", ".7322237.", ".7222227.", ".8724278."],
        ["...777...", "..77777..", "888888888", ".1222221.", ".2322232.", "..22422.."],
        ["..22222..", ".2222222.", ".2222222.", ".2322232.", ".1222221.", "..11411.."],
        ["...111...", "..11111..", ".1222221.", ".2322232.", ".2222222.", "..22422.."],
    ];

    private static readonly string[] Body =
    [
        "...222...",
        ".5556555.",
        "655565556",
        "655565556",
        "655565556",
        "655565556",
        "655565556",
    ];

    private static readonly Dictionary<PatronKey, PixelSprite> Cache = [];

    public static int HeadCount => Heads.Length;

    public static PixelSprite For(PatronLook look, Gaze gaze)
    {
        PatronKey key = new(look, gaze);
        if (Cache.TryGetValue(key, out PixelSprite? sprite))
        {
            return sprite;
        }

        if (Cache.Count >= CacheLimit)
        {
            Cache.Clear();
        }

        string[] rows = Heads[look.Head].Select(row => Turn(row, gaze)).Concat(Body).ToArray();
        sprite = PixelSprite.Parse(Colors(look), rows);
        Cache[key] = sprite;
        return sprite;
    }

    private static Dictionary<char, Color> Colors(PatronLook look) => new()
    {
        ['1'] = look.Hair,
        ['2'] = look.Skin,
        ['3'] = EyeColor,
        ['4'] = look.Skin.Darkened(0.22f),
        ['5'] = look.Clothes,
        ['6'] = look.Clothes.Darkened(0.3f),
        ['7'] = look.Accent,
        ['8'] = look.Accent.Darkened(0.3f),
    };

    private static string Turn(string row, Gaze gaze)
    {
        if (!row.Contains(Eye))
        {
            return row;
        }

        if (gaze == Gaze.Closed)
        {
            return row.Replace(Eye, Shade);
        }

        int step = gaze switch
        {
            Gaze.Left => -1,
            Gaze.Right => 1,
            _ => 0,
        };
        char[] turned = row.ToCharArray();
        for (int x = 0; x < row.Length; x++)
        {
            int target = x + step;
            if (step != 0 && row[x] == Eye && target >= 0 && target < row.Length && row[target] == Skin)
            {
                turned[x] = Skin;
                turned[target] = Eye;
            }
        }

        return new string(turned);
    }
}
