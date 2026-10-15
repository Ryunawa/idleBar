using System;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class ItemRow : PanelContainer
{
    private const int IconScale = 3;

    public static ItemRow Create(ItemData item, params (string Text, Action? Pressed)[] actions)
    {
        ItemRow row = new() { ThemeTypeVariation = WindowSkin.Card };
        HBoxContainer line = new();
        line.AddThemeConstantOverride("separation", 10);
        line.AddChild(new TextureRect
        {
            Texture = SpriteTexture.From(ItemSprites.Icon(item.Slot, item.Look, item.Rank, item.Class)),
            TextureFilter = TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(10 * IconScale, 10 * IconScale),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        });

        VBoxContainer text = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        Label name = new() { Text = item.Name };
        Rgba color = ItemSprites.Rarities[Math.Clamp(item.Rank, 0, ItemSprites.Rarities.Length - 1)].Main;
        name.AddThemeColorOverride("font_color", new Color(color.R / 255f, color.G / 255f, color.B / 255f));
        text.AddChild(name);
        string source = item.Source == "titan" ? " · tombé du titan" : string.Empty;
        string owner = item.Class is HeroRole role ? $" · pour les {Roles.Of(role).Name.ToLowerInvariant()}s" : string.Empty;
        text.AddChild(WindowRows.Muted($"{Items.SlotName(item.Slot)} · {Items.Required(item.Level)} · {item.Describe()}{owner}{source}"));
        line.AddChild(text);
        foreach ((string label, Action? pressed) in actions)
        {
            Button button = new() { Text = label, SizeFlagsVertical = SizeFlags.ShrinkCenter, Disabled = pressed is null };
            if (pressed is not null)
            {
                button.Pressed += pressed;
            }

            line.AddChild(button);
        }

        row.AddChild(line);
        return row;
    }
}
