using System;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class ShopRow : PanelContainer
{
    private const int IconScale = 3;

    private TextureRect _icon = null!;
    private Label _name = null!;
    private Label _detail = null!;
    private Button _buy = null!;
    private OfferData? _offer;

    public event Action<OfferData>? Bought;

    public override void _Ready()
    {
        ThemeTypeVariation = WindowSkin.Card;
        HBoxContainer line = new();
        line.AddThemeConstantOverride("separation", 10);
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
        _buy = new Button { CustomMinimumSize = new Vector2(170, 0), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _buy.Pressed += () =>
        {
            if (_offer is OfferData offer)
            {
                Bought?.Invoke(offer);
            }
        };
        line.AddChild(_icon);
        line.AddChild(text);
        line.AddChild(_buy);
        AddChild(line);
    }

    public void Show(OfferData offer)
    {
        _offer = offer;
        ItemData item = offer.ToItem();
        _name.Text = Items.Name(item.Slot, item.Look, item.Rank, item.Class);
        Rgba color = ItemSprites.Rarities[Math.Clamp(item.Rank, 0, ItemSprites.Rarities.Length - 1)].Main;
        _name.AddThemeColorOverride("font_color", new Color(color.R / 255f, color.G / 255f, color.B / 255f));
        string owner = item.Class is HeroRole role ? $" · pour les {Roles.Of(role).Name.ToLowerInvariant()}s" : string.Empty;
        _detail.Text = $"{Items.SlotName(item.Slot)} · {Items.Required(item.Level)} · {item.Describe()}{owner}";
        _icon.Texture = SpriteTexture.From(ItemSprites.Icon(item.Slot, item.Look, item.Rank, item.Class));
    }

    public void Refresh(double gold, bool idle, bool room)
    {
        if (_offer is not OfferData offer)
        {
            return;
        }

        _buy.Text = offer.Sold ? "Vendu" : $"Acheter · {NumberFormat.Gold(offer.Price)}";
        _buy.Disabled = offer.Sold || !idle || !room || gold < offer.Price;
        _buy.TooltipText = room ? "L'objet rejoint ton sac, lié à toi." : "Ton sac est plein.";
    }
}
