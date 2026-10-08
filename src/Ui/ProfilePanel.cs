using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Inn;
using IdleBar.Online;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class ProfilePanel : VBoxContainer
{
    private static readonly string[] Parts = ["Coiffure", "Peau", "Cheveux", "Vêtements", "Accessoire"];
    private static readonly string[] ColorNames = ["Ambre", "Rubis", "Émeraude", "Saphir", "Améthyste", "Or", "Rose", "Argent"];

    private readonly int[] _avatar = new int[Parts.Length];
    private readonly Label[] _values = new Label[Parts.Length];
    private TextureRect _portrait = null!;
    private OptionButton _bases = null!;
    private OptionButton _complements = null!;
    private OptionButton _drinks = null!;
    private OptionButton _colors = null!;
    private Label _specialty = null!;
    private IReadOnlyList<string> _baseWords = [];
    private IReadOnlyList<string> _complementWords = [];
    private IReadOnlyList<string> _menu = [];
    private bool _avatarEdited;
    private bool _specialtyEdited;

    public event Action<AvatarData>? AvatarSaved;

    public event Action<SpecialtyArguments>? SpecialtySaved;

    private static int[] Counts => [PatronSprites.HeadCount, PatronLook.SkinCount, PatronLook.HairCount, PatronLook.ClothesCount, PatronLook.AccentCount];

    public override void _Ready()
    {
        Name = "Avatar";
        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 6);
        content.AddChild(WindowRows.Heading("Ton avatar"));
        content.AddChild(WindowRows.Muted("C'est lui qui s'assoit au comptoir de tes amis quand tu leur rends visite."));
        HBoxContainer avatar = new();
        avatar.AddThemeConstantOverride("separation", 16);
        _portrait = new TextureRect
        {
            CustomMinimumSize = new Vector2(54, 78),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
        };
        avatar.AddChild(_portrait);
        VBoxContainer parts = new();
        for (int part = 0; part < Parts.Length; part++)
        {
            parts.AddChild(PartRow(part));
        }

        avatar.AddChild(parts);
        content.AddChild(avatar);
        content.AddChild(Save("Enregistrer l'avatar", () =>
        {
            _avatarEdited = false;
            AvatarSaved?.Invoke(new AvatarData(_avatar[0], _avatar[1], _avatar[2], _avatar[3], _avatar[4]));
        }));
        content.AddChild(WindowRows.Heading("Ta spécialité"));
        content.AddChild(WindowRows.Muted("Tes amis la goûtent quand ils viennent boire un verre chez toi, et l'ajoutent à leur carte des spécialités."));
        _bases = Choice(content, "Nom :");
        _complements = Choice(content, "Origine :");
        _drinks = Choice(content, "Servie comme :");
        _colors = Choice(content, "Couleur :");
        for (int color = 0; color < ColorNames.Length; color++)
        {
            Image swatch = Image.CreateEmpty(12, 12, false, Image.Format.Rgba8);
            swatch.Fill(SpecialtyColors.Of(color));
            _colors.AddIconItem(ImageTexture.CreateFromImage(swatch), ColorNames[color]);
        }

        _specialty = WindowRows.Heading(string.Empty);
        content.AddChild(_specialty);
        content.AddChild(Save("Enregistrer la spécialité", () =>
        {
            _specialtyEdited = false;
            SpecialtySaved?.Invoke(new SpecialtyArguments(
                _baseWords[Math.Max(_bases.Selected, 0)],
                _complementWords[Math.Max(_complements.Selected, 0)],
                _menu[Math.Max(_drinks.Selected, 0)],
                Math.Max(_colors.Selected, 0)));
        }));
        AddChild(WindowRows.Scroll(content));
    }

    public void Refresh(WorldData world, TavernData tavern)
    {
        if (_baseWords.Count == 0)
        {
            _baseWords = world.Bases;
            _complementWords = world.Complements;
            Fill(_bases, _baseWords);
            Fill(_complements, _complementWords);
        }

        if (!tavern.Menu.SequenceEqual(_menu))
        {
            _menu = tavern.Menu;
            Fill(_drinks, _menu.Select(id => DrinkMenu.FromId(id) is Drink drink ? DrinkMenu.Name(drink) : id).ToList());
            _specialtyEdited = false;
        }

        if (!_avatarEdited)
        {
            int[] values = [tavern.Avatar.Head, tavern.Avatar.Skin, tavern.Avatar.Hair, tavern.Avatar.Clothes, tavern.Avatar.Accent];
            values.CopyTo(_avatar, 0);
            ShowAvatar();
        }

        if (!_specialtyEdited)
        {
            _bases.Selected = Math.Max(0, IndexOf(_baseWords, tavern.Specialty.Base));
            _complements.Selected = Math.Max(0, IndexOf(_complementWords, tavern.Specialty.Complement));
            _drinks.Selected = Math.Max(0, IndexOf(_menu, tavern.Specialty.Drink));
            _colors.Selected = tavern.Specialty.Color;
            ShowSpecialty();
        }
    }

    private HBoxContainer PartRow(int part)
    {
        HBoxContainer row = new();
        row.AddChild(new Label { Text = Parts[part], CustomMinimumSize = new Vector2(110, 0) });
        row.AddChild(Save("<", () => Step(part, -1)));
        _values[part] = new Label { CustomMinimumSize = new Vector2(30, 0), HorizontalAlignment = HorizontalAlignment.Center };
        row.AddChild(_values[part]);
        row.AddChild(Save(">", () => Step(part, 1)));
        return row;
    }

    private void Step(int part, int direction)
    {
        _avatarEdited = true;
        _avatar[part] = (_avatar[part] + direction + Counts[part]) % Counts[part];
        ShowAvatar();
    }

    private void ShowAvatar()
    {
        for (int part = 0; part < Parts.Length; part++)
        {
            _values[part].Text = $"{_avatar[part] + 1}";
        }

        _portrait.Texture = PatronSprites.For(PatronLook.Compose(_avatar[0], _avatar[1], _avatar[2], _avatar[3], _avatar[4]), Gaze.Front).Texture;
    }

    private void ShowSpecialty()
    {
        if (_baseWords.Count > 0 && _complementWords.Count > 0)
        {
            _specialty.Text = $"{_baseWords[Math.Max(_bases.Selected, 0)]} {_complementWords[Math.Max(_complements.Selected, 0)]}";
            _specialty.AddThemeColorOverride("font_color", SpecialtyColors.Of(Math.Max(_colors.Selected, 0)));
        }
    }

    private OptionButton Choice(VBoxContainer content, string label)
    {
        HBoxContainer row = new();
        row.AddChild(new Label { Text = label, CustomMinimumSize = new Vector2(110, 0) });
        OptionButton choice = new() { FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        choice.ItemSelected += _ =>
        {
            _specialtyEdited = true;
            ShowSpecialty();
        };
        row.AddChild(choice);
        content.AddChild(row);
        return choice;
    }

    private static void Fill(OptionButton choice, IReadOnlyList<string> items)
    {
        choice.Clear();
        foreach (string item in items)
        {
            choice.AddItem(item);
        }
    }

    private static int IndexOf(IReadOnlyList<string> items, string value) => items.ToList().IndexOf(value);

    private static Button Save(string text, Action pressed)
    {
        Button button = new() { Text = text, FocusMode = FocusModeEnum.None };
        button.Pressed += pressed;
        return button;
    }
}
