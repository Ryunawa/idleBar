using System;
using System.Collections.Generic;
using Godot;

namespace IdleBar.Ui;

public static class ContractRow
{
    public static VBoxContainer AddScrollList(VBoxContainer owner)
    {
        ScrollContainer scroll = new() { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        VBoxContainer list = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        list.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(list);
        owner.AddChild(scroll);
        return list;
    }

    public static PanelContainer Create(ContractRowContent content) => Build(content, null);

    public static PanelContainer Create(ContractRowContent content, string action, bool enabled, Action onPressed)
    {
        Button button = new() { Text = action, Disabled = !enabled, FocusMode = Control.FocusModeEnum.None, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        button.Pressed += onPressed;
        return Build(content, button);
    }

    private static PanelContainer Build(ContractRowContent content, Button? button)
    {
        PanelContainer card = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, ThemeTypeVariation = WindowSkin.Card };
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 10);
        card.AddChild(row);
        if (GoodBadge.CreateIcon(content.GoodId) is TextureRect icon)
        {
            row.AddChild(icon);
        }

        VBoxContainer details = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        details.AddThemeConstantOverride("separation", 4);
        Label title = BarLabels.Create(14, BarPalette.Text);
        title.Text = content.Title;
        details.AddChild(title);
        if (content.Facts.Count > 0)
        {
            details.AddChild(CreateFacts(content.Facts));
        }

        if (content.Progress is double progress)
        {
            details.AddChild(CreateProgress(progress));
        }

        if (content.Note.Length > 0)
        {
            Label note = ActionRow.Note(content.Note);
            note.AddThemeColorOverride("font_color", content.NoteColor);
            details.AddChild(note);
        }

        row.AddChild(details);
        if (button is not null)
        {
            row.AddChild(button);
        }

        return card;
    }

    private static HFlowContainer CreateFacts(IReadOnlyList<string> facts)
    {
        HFlowContainer flow = new();
        flow.AddThemeConstantOverride("h_separation", 6);
        flow.AddThemeConstantOverride("v_separation", 4);
        foreach (string fact in facts)
        {
            PanelContainer chip = new() { ThemeTypeVariation = WindowSkin.Chip };
            Label label = BarLabels.Create(11, BarPalette.Muted);
            label.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            label.Text = fact;
            chip.AddChild(label);
            flow.AddChild(chip);
        }

        return flow;
    }

    private static ProgressBar CreateProgress(double progress)
    {
        ProgressBar bar = new()
        {
            MinValue = 0,
            MaxValue = 1,
            Step = 0.001,
            Value = progress,
            ShowPercentage = false,
            CustomMinimumSize = new Vector2(0, 10),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ThemeTypeVariation = progress >= 0.8 ? WindowSkin.WarningBar : string.Empty,
        };
        return bar;
    }
}
