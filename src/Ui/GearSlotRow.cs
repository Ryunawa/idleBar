using System;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class GearSlotRow : PanelContainer
{
    private const int IconScale = 3;

    private static readonly string[] RarityNames = ["Commune", "Rare", "Épique"];

    private readonly string _slot;
    private TextureRect _icon = null!;
    private Label _name = null!;
    private Label _detail = null!;
    private Button _upgrade = null!;
    private Button _max = null!;
    private Button _remove = null!;
    private OptionButton _rarity = null!;
    private Button _buy = null!;
    private ItemData? _item;
    private int _most;

    public GearSlotRow(string slot)
    {
        _slot = slot;
    }

    public event Action<ItemData, int>? Upgraded;

    public event Action<string>? Removed;

    public event Action<string, int>? Bought;

    public override void _Ready()
    {
        ThemeTypeVariation = WindowSkin.Card;
        VBoxContainer lines = new();
        lines.AddThemeConstantOverride("separation", 4);
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
        HBoxContainer shop = new();
        shop.AddThemeConstantOverride("separation", 10);
        shop.AddChild(new Label { Text = "Acheter neuve :", SizeFlagsHorizontal = SizeFlags.ExpandFill });
        _rarity = new OptionButton { CustomMinimumSize = new Vector2(120, 0) };
        for (int rank = 0; rank < RarityNames.Length; rank++)
        {
            _rarity.AddItem(RarityNames[rank], rank);
        }

        _rarity.Select(0);
        _buy = Make(() => Bought?.Invoke(_slot, _rarity.Selected));
        shop.AddChild(_rarity);
        shop.AddChild(_buy);
        lines.AddChild(worn);
        lines.AddChild(shop);
        AddChild(lines);
    }

    public void Show(ItemData? item, HeroRole role)
    {
        _item = item;
        string owner = _slot == Items.Weapon ? $" de {Roles.Of(role).Name.ToLowerInvariant()}" : string.Empty;
        _name.Text = item?.Name ?? $"{Items.SlotName(_slot)}{owner} : rien d'équipé";
        Rgba color = ItemSprites.Rarities[Math.Clamp(item?.Rank ?? 0, 0, ItemSprites.Rarities.Length - 1)].Main;
        _name.AddThemeColorOverride("font_color", item is null ? new Color(0.7f, 0.7f, 0.7f) : new Color(color.R / 255f, color.G / 255f, color.B / 255f));
        _detail.Text = item is null ? "Achète-la ci-dessous, ou équipe un objet du sac." : $"Niveau {item.Level} · {item.Describe()}{(item.Bound ? " · liée" : string.Empty)}";
        _icon.Texture = item is null ? null : SpriteTexture.From(ItemSprites.Icon(item.Slot, item.Look, item.Rank, item.Class));
        _upgrade.Visible = _max.Visible = _remove.Visible = item is not null;
    }

    public void Refresh(FrontierState? state, bool idle, bool room)
    {
        if (_item is ItemData item && state is not null)
        {
            bool full = item.Upgrades >= Balance.MaxUpgrades;
            _most = GearPrices.MaxUpgrades(state, item.Rank, item.Upgrades);
            _upgrade.Text = full ? "Au maximum" : $"Améliorer · {NumberFormat.Gold(GearPrices.Upgrade(state, item.Rank, item.Upgrades, 1))}";
            _upgrade.Disabled = !idle || full || _most < 1;
            _max.Text = $"Max ×{_most}";
            _max.Disabled = !idle || _most < 1;
            _remove.Disabled = !idle;
        }

        if (state is not null)
        {
            double price = GearPrices.Buy(state, _rarity.Selected);
            _buy.Text = $"Acheter · {NumberFormat.Gold(price)}";
            _buy.Disabled = !idle || !room || state.Gold < price;
            _buy.TooltipText = room ? $"Une pièce neuve du niveau {state.MaxStage}, tirée par le serveur et liée à toi." : "Ton sac est plein.";
        }
    }

    private static Button Make(Action pressed)
    {
        Button button = new() { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        button.Pressed += pressed;
        return button;
    }
}
