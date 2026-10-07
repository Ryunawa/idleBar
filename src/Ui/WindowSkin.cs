using System.Collections.Generic;
using Godot;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public static class WindowSkin
{
    public const string Card = "Card";
    public const string Chip = "Chip";
    public const string WarningBar = "WarningBar";

    private static readonly Dictionary<string, Theme> Skins = [];

    public static Theme For(string townId)
    {
        if (!Skins.TryGetValue(townId, out Theme? skin))
        {
            skin = Create(WindowStyles.For(townId));
            Skins[townId] = skin;
        }

        return skin;
    }

    public static Theme Create(WindowStyle style)
    {
        Color outline = style.WoodDark.Darkened(0.45f);
        Theme theme = new() { DefaultFont = PixelFont.Load(), DefaultFontSize = PixelFont.Body };
        theme.SetColor("font_color", "Label", BarPalette.Text);

        theme.SetStylebox("panel", "Panel", PixelFrame.Box(style.Panel,
            [PixelRing.Solid(outline), new PixelRing(style.WoodLight, style.WoodDark), PixelRing.Solid(style.Wood),
                PixelRing.Solid(style.Wood), new PixelRing(style.WoodDark, style.WoodLight)], 0, style.Accent));

        AddTabs(theme, style, outline);
        AddButtons(theme, style, outline);
        AddFields(theme, style, outline);

        theme.SetTypeVariation(Card, "PanelContainer");
        theme.SetStylebox("panel", Card, PixelFrame.Raised(style.Card, style.Card.Lightened(0.18f), style.PanelDeep, outline, 8));
        theme.SetTypeVariation(Chip, "PanelContainer");
        theme.SetStylebox("panel", Chip, PixelFrame.Inset(style.PanelDeep, style.Card, outline, outline, 4));

        theme.SetStylebox("background", "ProgressBar", PixelFrame.Inset(style.PanelDeep, style.Card, outline, outline, 0));
        theme.SetStylebox("fill", "ProgressBar", PixelFrame.Raised(style.Accent, style.Accent.Lightened(0.35f), style.Accent.Darkened(0.35f), outline, 0));
        theme.SetTypeVariation(WarningBar, "ProgressBar");
        theme.SetStylebox("fill", WarningBar, PixelFrame.Raised(BarPalette.Warning, BarPalette.Warning.Lightened(0.35f), BarPalette.Warning.Darkened(0.35f), outline, 0));

        theme.SetStylebox("separator", "HSeparator", new StyleBoxLine { Color = style.WoodDark, Thickness = PixelFrame.Scale * 2 });
        theme.SetConstant("separation", "HSeparator", PixelFrame.Scale * 4);

        theme.SetConstant("scrollbar_v_separation", "ScrollContainer", 6);
        theme.SetStylebox("scroll", "VScrollBar", PixelFrame.Inset(style.PanelDeep, style.Card, outline, outline, 0));
        theme.SetStylebox("grabber", "VScrollBar", PixelFrame.Raised(style.Wood, style.WoodLight, style.WoodDark, outline, 0));
        theme.SetStylebox("grabber_highlight", "VScrollBar", PixelFrame.Raised(style.WoodLight, style.WoodLight.Lightened(0.2f), style.WoodDark, outline, 0));
        theme.SetStylebox("grabber_pressed", "VScrollBar", PixelFrame.Inset(style.WoodDark, style.WoodLight, outline, outline, 0));

        theme.SetStylebox("panel", "TooltipPanel", PixelFrame.Raised(style.PanelDeep, style.WoodLight, style.WoodDark, outline, 6));
        theme.SetColor("font_color", "TooltipLabel", BarPalette.Text);
        return theme;
    }

    private static void AddTabs(Theme theme, WindowStyle style, Color outline)
    {
        const string Tabs = "TabContainer";
        PixelRing frame = new(style.WoodLight, style.WoodDark);
        theme.SetStylebox("panel", Tabs, PixelFrame.Box(style.Panel, [PixelRing.Solid(outline), new PixelRing(style.WoodDark, style.WoodLight)], 8));
        theme.SetStylebox("tab_selected", Tabs, PixelFrame.Box(style.Panel, [PixelRing.Solid(outline), frame], 8, openBottom: true));
        theme.SetStylebox("tab_unselected", Tabs, PixelFrame.Box(style.PanelDeep, [PixelRing.Solid(outline), new PixelRing(style.Card, outline)], 8));
        theme.SetStylebox("tab_hovered", Tabs, PixelFrame.Box(style.Card, [PixelRing.Solid(outline), frame], 8));
        theme.SetStylebox("tab_disabled", Tabs, PixelFrame.Box(style.PanelDeep, [PixelRing.Solid(outline), PixelRing.Solid(style.PanelDeep)], 8));
        theme.SetStylebox("tabbar_background", Tabs, new StyleBoxEmpty());
        theme.SetColor("font_selected_color", Tabs, style.Accent);
        theme.SetColor("font_unselected_color", Tabs, BarPalette.Muted);
        theme.SetColor("font_hovered_color", Tabs, BarPalette.Text);
        theme.SetConstant("side_margin", Tabs, 0);
    }

    private static void AddButtons(Theme theme, WindowStyle style, Color outline)
    {
        const string Button = "Button";
        theme.SetStylebox("normal", Button, PixelFrame.Raised(style.Wood, style.WoodLight, style.WoodDark, outline, 8));
        theme.SetStylebox("hover", Button, PixelFrame.Raised(style.WoodLight, style.WoodLight.Lightened(0.25f), style.WoodDark, outline, 8));
        theme.SetStylebox("pressed", Button, PixelFrame.Inset(style.WoodDark, style.WoodLight, outline, outline, 8));
        theme.SetStylebox("disabled", Button, PixelFrame.Raised(style.PanelDeep, style.Card, style.PanelDeep, outline, 8));
        theme.SetStylebox("focus", Button, new StyleBoxEmpty());
        theme.SetColor("font_color", Button, BarPalette.Text);
        theme.SetColor("font_hover_color", Button, BarPalette.Text);
        theme.SetColor("font_pressed_color", Button, style.Accent);
        theme.SetColor("font_hover_pressed_color", Button, style.Accent);
        theme.SetColor("font_disabled_color", Button, BarPalette.Muted.Darkened(0.25f));
        theme.SetIcon("arrow", "OptionButton", PixelFrame.Arrow(BarPalette.Text));
        theme.SetConstant("arrow_margin", "OptionButton", 12);
        theme.SetStylebox("panel", "PopupMenu", PixelFrame.Raised(style.PanelDeep, style.WoodLight, style.WoodDark, outline, 6));
        theme.SetStylebox("hover", "PopupMenu", PixelFrame.Raised(style.Card, style.Card.Lightened(0.2f), style.PanelDeep, outline, 4));
        theme.SetColor("font_color", "PopupMenu", BarPalette.Text);
        theme.SetColor("font_hover_color", "PopupMenu", style.Accent);
    }

    private static void AddFields(Theme theme, WindowStyle style, Color outline)
    {
        const string LineEdit = "LineEdit";
        theme.SetStylebox("normal", LineEdit, PixelFrame.Inset(style.PanelDeep, style.Card, outline, outline, 6));
        theme.SetStylebox("focus", LineEdit, PixelFrame.Inset(style.PanelDeep, style.Accent, style.Accent.Darkened(0.4f), outline, 6));
        theme.SetStylebox("read_only", LineEdit, PixelFrame.Inset(style.Panel, style.Card, outline, outline, 6));
        theme.SetColor("font_color", LineEdit, BarPalette.Text);
        theme.SetColor("font_placeholder_color", LineEdit, BarPalette.Muted);
        theme.SetColor("caret_color", LineEdit, style.Accent);
        theme.SetColor("selection_color", LineEdit, style.Accent with { A = 0.35f });
        AddSpinBox(theme, style, outline);
    }

    private static void AddSpinBox(Theme theme, WindowStyle style, Color outline)
    {
        const string SpinBox = "SpinBox";
        foreach ((string part, bool up) in new[] { ("up", true), ("down", false) })
        {
            theme.SetIcon(part, SpinBox, PixelFrame.Arrow(BarPalette.Text, up, small: true));
            theme.SetIcon($"{part}_hover", SpinBox, PixelFrame.Arrow(BarPalette.Text, up, small: true));
            theme.SetIcon($"{part}_pressed", SpinBox, PixelFrame.Arrow(style.Accent, up, small: true));
            theme.SetIcon($"{part}_disabled", SpinBox, PixelFrame.Arrow(BarPalette.Muted.Darkened(0.25f), up, small: true));
            theme.SetStylebox($"{part}_background", SpinBox, PixelFrame.Raised(style.Wood, style.WoodLight, style.WoodDark, outline, 0));
            theme.SetStylebox($"{part}_background_hovered", SpinBox, PixelFrame.Raised(style.WoodLight, style.WoodLight.Lightened(0.25f), style.WoodDark, outline, 0));
            theme.SetStylebox($"{part}_background_pressed", SpinBox, PixelFrame.Inset(style.WoodDark, style.WoodLight, outline, outline, 0));
            theme.SetStylebox($"{part}_background_disabled", SpinBox, PixelFrame.Raised(style.PanelDeep, style.Card, style.PanelDeep, outline, 0));
        }

        theme.SetStylebox("field_and_buttons_separator", SpinBox, new StyleBoxEmpty());
        theme.SetStylebox("up_down_buttons_separator", SpinBox, new StyleBoxEmpty());
        theme.SetConstant("buttons_width", SpinBox, 22);
        theme.SetConstant("buttons_vertical_separation", SpinBox, 2);
        theme.SetConstant("field_and_buttons_separation", SpinBox, 4);
        theme.SetConstant("set_min_buttons_width_from_icons", SpinBox, 0);
    }
}
