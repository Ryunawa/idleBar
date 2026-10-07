using Godot;

namespace IdleBar.Ui;

public static class GoodBadge
{
    private const string IconFolder = "res://assets/goods";
    private const int IconSize = 32;

    public static HBoxContainer Create(string goodId, string name, int fontSize, Color color)
    {
        HBoxContainer badge = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
        badge.AddThemeConstantOverride("separation", 6);
        if (CreateIcon(goodId) is TextureRect icon)
        {
            badge.AddChild(icon);
        }

        Label label = BarLabels.Create(fontSize, color);
        label.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        label.Text = name;
        badge.AddChild(label);
        return badge;
    }

    public static TextureRect? CreateIcon(string goodId)
    {
        string path = $"{IconFolder}/{goodId}.png";
        if (!ResourceLoader.Exists(path))
        {
            return null;
        }

        return new TextureRect
        {
            Texture = GD.Load<Texture2D>(path),
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
    }
}
