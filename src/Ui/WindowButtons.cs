using System;
using Godot;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public static class WindowButtons
{
    private static readonly Vector2 ButtonSize = new(22, 18);
    private static readonly string[] ButtonStates = ["normal", "hover", "pressed", "focus"];

    public static void Add(BoxContainer container, string toggleText, Action onToggle, Action onQuit)
    {
        container.AddThemeConstantOverride("separation", 2);
        container.AddChild(Create(toggleText, "Replier / déplier", onToggle));
        container.AddChild(Create("×", "Quitter", onQuit));
    }

    public static Button CreateSettings(Action onPressed)
    {
        Button button = Create(string.Empty, "Réglages : taille et écran de la barre", onPressed);
        button.Icon = IconSprites.Gear.Texture;
        button.ExpandIcon = true;
        button.IconAlignment = HorizontalAlignment.Center;
        button.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;
        button.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        StyleBoxEmpty tight = new() { ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 2, ContentMarginBottom = 2 };
        foreach (string state in ButtonStates)
        {
            button.AddThemeStyleboxOverride(state, tight);
        }

        return button;
    }

    private static Button Create(string text, string tooltip, Action onPressed)
    {
        Button button = new()
        {
            Text = text,
            TooltipText = tooltip,
            Flat = true,
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = ButtonSize,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        ClickBinding.OnLeftPress(button, onPressed);
        return button;
    }
}
