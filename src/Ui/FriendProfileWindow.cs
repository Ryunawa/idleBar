using System;
using System.Linq;
using Godot;
using IdleBar.Inn;
using IdleBar.Online;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class FriendProfileWindow : Window
{
    private static readonly Vector2I BaseSize = new(460, 440);
    private static readonly Vector2 PortraitSize = new(54, 78);
    private static readonly Vector2 StampSize = new(28, 24);

    private VBoxContainer _rows = null!;
    private Button _mute = null!;
    private FriendProfile? _profile;
    private WorldData? _world;

    public event Action<Guid, bool>? MuteRequested;

    public override void _Ready()
    {
        VBoxContainer content = WindowFrame.Build(this, "IdleBar · Fiche d'ami", 6);
        _rows = new VBoxContainer();
        _rows.AddThemeConstantOverride("separation", 6);
        content.AddChild(WindowRows.Scroll(_rows));
        HBoxContainer buttons = new();
        buttons.AddThemeConstantOverride("separation", 8);
        _mute = new Button { FocusMode = Control.FocusModeEnum.None, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _mute.Pressed += () =>
        {
            if (_profile is FriendProfile profile)
            {
                MuteRequested?.Invoke(profile.Id, !profile.Muted);
                Show(profile with { Muted = !profile.Muted }, null);
            }
        };
        Button close = new() { Text = "Fermer", FocusMode = Control.FocusModeEnum.None, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        close.Pressed += Hide;
        buttons.AddChild(_mute);
        buttons.AddChild(close);
        content.AddChild(buttons);
    }

    public void Open(float scale, FriendProfile profile, WorldData world)
    {
        Show(profile, world);
        WindowFrame.Present(this, BaseSize, scale);
    }

    private void Show(FriendProfile profile, WorldData? world)
    {
        _profile = profile;
        _world = world ?? _world;
        Title = $"IdleBar · {profile.Name}";
        _mute.Text = profile.Muted ? "Afficher ses messages" : "Masquer ses messages";
        WindowRows.Clear(_rows);

        HBoxContainer identity = new();
        identity.AddThemeConstantOverride("separation", 14);
        identity.AddChild(Picture(PatronSprites.For(VisitDesk.Look(profile.Avatar), Gaze.Front).Texture, PortraitSize));
        VBoxContainer about = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        about.AddThemeConstantOverride("separation", 2);
        about.AddChild(WindowRows.Heading(profile.Name));
        string tier = _world?.Tiers.FirstOrDefault(each => each.Tier == profile.Tier)?.Name ?? string.Empty;
        Label status = new() { Text = profile.Online ? $"{tier} · en ligne" : tier };
        status.AddThemeColorOverride("font_color", profile.Online ? BarPalette.Success : BarPalette.Text);
        about.AddChild(status);
        about.AddChild(WindowRows.Muted($"{NumberFormat.Amount(profile.Served)} clients servis · {Stories(profile.Souvenirs)}"));
        if (profile.FriendsSince is DateTimeOffset since)
        {
            about.AddChild(WindowRows.Muted($"Amis depuis le {since.ToLocalTime():dd/MM/yyyy}"));
        }

        identity.AddChild(about);
        _rows.AddChild(WindowRows.Card(identity));

        _rows.AddChild(WindowRows.Heading("Son tampon et sa spécialité"));
        HBoxContainer signature = new();
        signature.AddThemeConstantOverride("separation", 10);
        signature.AddChild(Picture(StampSprites.For(profile.Stamp)?.Texture, StampSize));
        signature.AddChild(Swatch(profile.Specialty.Color));
        signature.AddChild(new Label { Text = $"{profile.Specialty.Name}, servie comme {DrinkName(profile.Specialty.Drink)}" });
        _rows.AddChild(signature);

        _rows.AddChild(WindowRows.Heading($"Son livre d'or · {profile.Guestbook.Count} visiteurs"));
        if (profile.Guestbook.Count == 0)
        {
            _rows.AddChild(WindowRows.Muted("Personne n'est encore venu boire un verre chez lui."));
        }

        foreach (GuestbookEntry entry in profile.Guestbook)
        {
            HBoxContainer row = new();
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(Picture(StampSprites.For(entry.Stamp)?.Texture, StampSize));
            string visits = entry.Visits > 1 ? $" · {entry.Visits} visites" : string.Empty;
            row.AddChild(new Label { Text = $"{entry.Name}{visits}", SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
            _rows.AddChild(row);
        }

        _rows.AddChild(WindowRows.Heading($"Sa carte des spécialités · {profile.Tasted.Count} goûtées"));
        foreach (ShownSpecialty tasted in profile.Tasted)
        {
            HBoxContainer row = new();
            row.AddThemeConstantOverride("separation", 8);
            row.AddChild(Swatch(tasted.Color));
            row.AddChild(new Label { Text = $"{tasted.Name}, servie comme {DrinkName(tasted.Drink)}" });
            _rows.AddChild(row);
        }
    }

    private static string Stories(int souvenirs) => souvenirs switch
    {
        0 => "aucune histoire d'habitué terminée",
        1 => "une histoire d'habitué terminée",
        _ => $"{souvenirs} histoires d'habitués terminées",
    };

    private static string DrinkName(string drink) => DrinkMenu.FromId(drink) is Drink known ? DrinkMenu.Name(known).ToLowerInvariant() : drink;

    private static ColorRect Swatch(int color) => new()
    {
        Color = SpecialtyColors.Of(color),
        CustomMinimumSize = new Vector2(12, 12),
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
    };

    private static TextureRect Picture(Texture2D? texture, Vector2 size) => new()
    {
        Texture = texture,
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
    };
}
