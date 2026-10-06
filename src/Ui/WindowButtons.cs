using System;
using Godot;

namespace IdleBar.Ui;

public static class WindowButtons
{
    public static void Add(BoxContainer container, string toggleText, Action onToggle, Action onQuit)
    {
        container.AddThemeConstantOverride("separation", 2);
        container.AddChild(Create(toggleText, "Replier / déplier", onToggle));
        container.AddChild(Create("×", "Quitter", onQuit));
    }

    private static Button Create(string text, string tooltip, Action onPressed)
    {
        Button button = new()
        {
            Text = text,
            TooltipText = tooltip,
            Flat = true,
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(22, 18),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand,
        };
        ClickBinding.OnLeftPress(button, onPressed);
        return button;
    }
}
