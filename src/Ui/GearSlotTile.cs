using System;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class GearSlotTile : Button
{
    private const int IconScale = 3;

    private TextureRect _icon = null!;
    private Label _label = null!;

    public GearSlotTile(string slot)
    {
        Slot = slot;
        ToggleMode = true;
    }

    public string Slot { get; }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(88, 64);
        VBoxContainer content = new() { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
        content.AddThemeConstantOverride("separation", 2);
        _icon = new TextureRect
        {
            TextureFilter = TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(10 * IconScale, 10 * IconScale),
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _label = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        _label.AddThemeFontSizeOverride("font_size", PixelFont.Size(12));
        content.AddChild(_icon);
        content.AddChild(_label);
        AddChild(content);
        content.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public void Show(ItemData? item, int ready, HeroRole role)
    {
        string name = Items.SlotName(Slot);
        _icon.Texture = SpriteTexture.From(item is null
            ? ItemSprites.Icon(Slot, 0, 0, role).Faded(Palette.Brown, 0.75f)
            : ItemSprites.Icon(item.Slot, item.Look, item.Rank, item.Class));
        _label.Text = ready > 0 ? $"{name} · {ready}" : name;
        Rgba color = ItemSprites.Rarities[Math.Clamp(item?.Rank ?? 0, 0, ItemSprites.Rarities.Length - 1)].Main;
        _label.AddThemeColorOverride("font_color", item is null ? BarPalette.Muted : new Color(color.R / 255f, color.G / 255f, color.B / 255f));
        string worn = item is null ? $"{name} : rien d'équipé" : $"{item.Name}\n{Items.Required(item.Level)} · {item.Describe()}";
        TooltipText = ready > 0 ? $"{worn}\n{ready} objet{(ready > 1 ? "s" : string.Empty)} du sac {(ready > 1 ? "peuvent" : "peut")} aller ici" : worn;
    }
}
