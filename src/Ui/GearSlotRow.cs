using System;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class GearSlotRow : PanelContainer
{
    private const int IconScale = 3;

    private readonly string _slot;
    private TextureRect _icon = null!;
    private Label _name = null!;
    private Label _detail = null!;
    private Button _upgrade = null!;
    private Button _max = null!;
    private Button _remove = null!;
    private ItemData? _item;
    private int _most;

    public GearSlotRow(string slot)
    {
        _slot = slot;
    }

    public event Action<ItemData, int>? Upgraded;

    public event Action<string>? Removed;

    public override void _Ready()
    {
        ThemeTypeVariation = WindowSkin.Card;
        HBoxContainer worn = new();
        worn.AddThemeConstantOverride("separation", 10);
        _icon = new TextureRect
        {
            TextureFilter = TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(10 * IconScale, 10 * IconScale),
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        VBoxContainer text = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _name = new Label();
        _detail = WindowRows.Muted(string.Empty);
        text.AddChild(_name);
        text.AddChild(_detail);
        _upgrade = Make(() => Upgraded?.Invoke(_item!, 1));
        _max = Make(() => Upgraded?.Invoke(_item!, _most));
        _remove = Make(() => Removed?.Invoke(_slot));
        _remove.Text = "Retirer";
        worn.AddChild(_icon);
        worn.AddChild(text);
        worn.AddChild(_upgrade);
        worn.AddChild(_max);
        worn.AddChild(_remove);
        AddChild(worn);
    }

    public void Show(ItemData? item, HeroRole role)
    {
        _item = item;
        string owner = _slot == Items.Weapon ? $" de {Roles.Of(role).Name.ToLowerInvariant()}" : string.Empty;
        _name.Text = item?.Name ?? $"{Items.SlotName(_slot)}{owner} : rien d'équipé";
        Rgba color = ItemSprites.Rarities[Math.Clamp(item?.Rank ?? 0, 0, ItemSprites.Rarities.Length - 1)].Main;
        _name.AddThemeColorOverride("font_color", item is null ? new Color(0.7f, 0.7f, 0.7f) : new Color(color.R / 255f, color.G / 255f, color.B / 255f));
        _detail.Text = item is null ? "Équipe un objet du sac, ou guette la boutique." : $"Niveau {item.Level} · {item.Describe()}{(item.Bound ? " · liée" : string.Empty)}";
        _icon.Texture = item is null ? null : SpriteTexture.From(ItemSprites.Icon(item.Slot, item.Look, item.Rank, item.Class));
        _upgrade.Visible = _max.Visible = _remove.Visible = item is not null;
    }

    public void Refresh(FrontierState? state, bool idle)
    {
        if (_item is not ItemData item || state is null)
        {
            return;
        }

        bool full = item.Upgrades >= Balance.MaxUpgrades;
        _most = GearPrices.MaxUpgrades(state, item.Rank, item.Upgrades);
        _upgrade.Text = full ? "Au maximum" : $"Améliorer · {NumberFormat.Gold(GearPrices.Upgrade(state, item.Rank, item.Upgrades, 1))}";
        _upgrade.Disabled = !idle || full || _most < 1;
        _max.Text = $"Max ×{_most}";
        _max.Disabled = !idle || _most < 1;
        _remove.Disabled = !idle;
    }

    private static Button Make(Action pressed)
    {
        Button button = new() { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        button.Pressed += pressed;
        return button;
    }
}
