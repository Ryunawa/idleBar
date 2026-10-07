using System;
using Godot;

namespace IdleBar.Ui;

public static class BatchPicker
{
    public static HBoxContainer Create(string caption, int[] choices, int maxChoice, Action<int> onChosen)
    {
        HBoxContainer picker = new();
        picker.AddThemeConstantOverride("separation", 6);
        Label label = BarLabels.Create(12, BarPalette.Muted);
        label.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        label.Text = caption;
        picker.AddChild(label);

        ButtonGroup group = new();
        foreach (int choice in choices)
        {
            Button option = new()
            {
                Text = choice == maxChoice ? "Max" : $"×{NumberFormat.Amount(choice)}",
                ToggleMode = true,
                ButtonGroup = group,
                ButtonPressed = choice == choices[0],
                FocusMode = Control.FocusModeEnum.None,
            };
            option.Pressed += () => onChosen(choice);
            picker.AddChild(option);
        }

        return picker;
    }
}
