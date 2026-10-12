using System;
using IdleBar.Frontier;

namespace IdleBar.Scene;

public sealed record MonsterBox(int Template, bool Boss, int Stage, int X, int Y, int Width, int Height)
{
    public const int SlimeWidth = 12;
    public const int SlimeHeight = 9;

    public int CenterX => X + Width / 2;

    public int MiddleY => Y + Height / 2;

    public static MonsterBox Of(Monster monster, FrontierLayout layout, bool kicked)
    {
        ArtSprite? sprite = SpriteOf(monster.Template, monster.Boss);
        int width = sprite?.Width ?? SlimeWidth;
        int height = sprite?.Height ?? SlimeHeight;
        int stand = monster.Boss ? layout.BossX : layout.MonsterX;
        double share = monster.Approach > 0 ? monster.Approach / monster.ApproachTotal : 0;
        int x = (int)Math.Round(stand + (layout.SpawnX - stand) * share) + (kicked ? 1 : 0);
        return new MonsterBox(monster.Template, monster.Boss, monster.Stage, x, FrontierLayout.Ground - height, width, height);
    }

    public static ArtSprite? SpriteOf(int template, bool boss) => boss ? SceneSprites.Bosses[template] : SceneSprites.Monsters[template];
}
