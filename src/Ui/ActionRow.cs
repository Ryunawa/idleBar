using System;
using Godot;

namespace IdleBar.Ui;

public static class ActionRow
{
    public static HBoxContainer Create(string title, string detail, string action, bool disabled, Action onPressed)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 12);

        VBoxContainer details = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        details.AddThemeConstantOverride("separation", 0);
        Label name = BarLabels.Create(14, BarPalette.Text);
        name.Text = title;
        details.AddChild(name);
        Label description = BarLabels.Create(11, BarPalette.Muted);
        description.Text = detail;
        description.TooltipText = detail;
        description.MouseFilter = Control.MouseFilterEnum.Pass;
        details.AddChild(description);
        row.AddChild(details);

        if (action.Length > 0)
        {
            Button button = new() { Text = action, Disabled = disabled, FocusMode = Control.FocusModeEnum.None, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
            button.Pressed += onPressed;
            row.AddChild(button);
        }

        return row;
    }

    public static Label Heading(string text)
    {
        Label heading = BarLabels.Create(12, BarPalette.Gold);
        heading.Text = text;
        return heading;
    }

    public static Label Note(string text)
    {
        Label note = new() { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
        note.AddThemeFontSizeOverride("font_size", PixelFont.Size(11));
        note.AddThemeColorOverride("font_color", BarPalette.Muted);
        return note;
    }

    public static void Clear(Node container)
    {
        foreach (Node child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }
}
