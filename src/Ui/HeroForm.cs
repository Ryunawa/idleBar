using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Godot;
using IdleBar.Frontier;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class HeroForm : HBoxContainer
{
    private const int PreviewScale = 6;

    private static readonly string[] SkinNames = ["Claire", "Dorée", "Mate", "Foncée"];
    private static readonly string[] HairNames = ["Courts", "Longs", "En queue"];
    private static readonly string[] HairColorNames = ["Noirs", "Bruns", "Roux", "Blonds", "Blancs", "Roses"];
    private static readonly string[] OutfitNames = ["Rouge", "Bleue", "Verte", "Rose", "Orange", "Jaune", "Lavande", "Noire"];
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    private TextureRect _preview = null!;
    private LineEdit _name = null!;
    private OptionButton _role = null!;
    private OptionButton _skin = null!;
    private OptionButton _hair = null!;
    private OptionButton _hairColor = null!;
    private OptionButton _outfit = null!;
    private Label _roleText = null!;

    public event Action? Submitted;

    public HeroLook Look => new(HeroLook.Tidy(_name.Text), _skin.Selected, _hair.Selected, _hairColor.Selected, _outfit.Selected);

    public HeroRole Role => (HeroRole)Math.Max(0, _role.Selected);

    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 12);
        _preview = new TextureRect
        {
            TextureFilter = TextureFilterEnum.Nearest,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            CustomMinimumSize = new Vector2(12 * PreviewScale, 17 * PreviewScale),
            SizeFlagsVertical = SizeFlags.ShrinkBegin,
        };
        AddChild(_preview);

        VBoxContainer fields = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        fields.AddThemeConstantOverride("separation", 6);
        GridContainer grid = new() { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 4);
        _name = new LineEdit { MaxLength = HeroLook.NameMax, PlaceholderText = "Nom du héros", SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _name.TextSubmitted += _ => Submitted?.Invoke();
        Row(grid, "Nom", _name);
        _role = Choice(grid, "Rôle", Roles.All.Select(role => role.Name));
        _skin = Choice(grid, "Peau", SkinNames);
        _hair = Choice(grid, "Cheveux", HairNames);
        _hairColor = Choice(grid, "Couleur", HairColorNames);
        _outfit = Choice(grid, "Tenue", OutfitNames);
        fields.AddChild(grid);
        _roleText = WindowRows.Muted(string.Empty);
        fields.AddChild(_roleText);
        AddChild(fields);
    }

    public void Fill(HeroLook? look, HeroRole role, bool roleLocked)
    {
        HeroLook shown = look ?? HeroSprites.Default;
        _name.Text = look?.Name ?? string.Empty;
        _role.Select((int)role);
        _role.Disabled = roleLocked;
        _skin.Select(shown.Skin);
        _hair.Select(shown.Hair);
        _hairColor.Select(shown.HairColor);
        _outfit.Select(shown.Outfit);
        Update();
    }

    private void Update()
    {
        _preview.Texture = SpriteTexture.From(HeroSprites.Of(Role, Look).Standing);
        RoleInfo info = Roles.Of(Role);
        string locked = _role.Disabled ? " Le rôle se change au prochain repli." : string.Empty;
        _roleText.Text = $"{info.Name} : {info.Effect} PV ×{info.Hp.ToString("0.##", French)} · dégâts ×{info.Dps.ToString("0.##", French)}.{locked}";
    }

    private OptionButton Choice(GridContainer grid, string label, IEnumerable<string> items)
    {
        OptionButton choice = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (string item in items)
        {
            choice.AddItem(item);
        }

        choice.ItemSelected += _ => Update();
        Row(grid, label, choice);
        return choice;
    }

    private static void Row(GridContainer grid, string label, Control field)
    {
        grid.AddChild(new Label { Text = label });
        grid.AddChild(field);
    }
}
