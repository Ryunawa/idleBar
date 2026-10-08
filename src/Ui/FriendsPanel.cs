using System;
using System.Linq;
using Godot;
using IdleBar.Online;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public partial class FriendsPanel : VBoxContainer
{
    private static readonly Vector2 PortraitSize = new(24, 34);

    private Label _code = null!;
    private LineEdit _friendCode = null!;
    private OptionButton _stamps = null!;
    private VBoxContainer _rows = null!;
    private WorldData? _world;

    public event Action<string>? FriendRequested;

    public event Action<Guid, bool>? Answered;

    public event Action<Guid>? Removed;

    public event Action<Guid, string>? VisitRequested;

    public override void _Ready()
    {
        Name = "Amis";
        VBoxContainer content = new();
        content.AddThemeConstantOverride("separation", 6);
        _code = WindowRows.Heading(string.Empty);
        content.AddChild(_code);
        HBoxContainer add = new();
        _friendCode = new LineEdit { PlaceholderText = "Code d'un ami", MaxLength = 6, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _friendCode.TextSubmitted += _ => RequestFriend();
        add.AddChild(_friendCode);
        Button request = new() { Text = "Ajouter", FocusMode = FocusModeEnum.None };
        request.Pressed += RequestFriend;
        add.AddChild(request);
        content.AddChild(add);
        HBoxContainer stamp = new();
        stamp.AddChild(new Label { Text = "Ton tampon pour leur livre d'or :" });
        _stamps = new OptionButton { FocusMode = FocusModeEnum.None };
        stamp.AddChild(_stamps);
        content.AddChild(stamp);
        _rows = new VBoxContainer();
        _rows.AddThemeConstantOverride("separation", 6);
        content.AddChild(_rows);
        AddChild(WindowRows.Scroll(content));
    }

    public void Refresh(WorldData world, TavernData tavern)
    {
        if (_world is null)
        {
            _world = world;
            foreach (StampInfo stamp in world.Stamps)
            {
                _stamps.AddIconItem(StampSprites.For(stamp.Id)?.Texture, stamp.Name);
            }
        }

        _code.Text = $"Ton code ami : {tavern.FriendCode}";
        WindowRows.Clear(_rows);
        foreach (RequestData request in tavern.Requests)
        {
            HBoxContainer row = new();
            row.AddChild(new Label { Text = $"{request.Name} veut être ton ami", SizeFlagsHorizontal = SizeFlags.ExpandFill });
            row.AddChild(Action("Accepter", () => Answered?.Invoke(request.Id, true)));
            row.AddChild(Action("Refuser", () => Answered?.Invoke(request.Id, false)));
            _rows.AddChild(WindowRows.Card(row));
        }

        _rows.AddChild(WindowRows.Heading(tavern.Friends.Count == 0 ? "Pas encore d'amis : échange vos codes." : $"Tes amis · {tavern.Friends.Count}"));
        foreach (FriendData friend in tavern.Friends)
        {
            _rows.AddChild(FriendRow(friend, tavern.Outing is null));
        }

        _rows.AddChild(WindowRows.Heading($"Ton livre d'or · {tavern.Guestbook.Count}"));
        foreach (GuestbookEntry entry in tavern.Guestbook)
        {
            HBoxContainer row = new();
            row.AddChild(Icon(StampSprites.For(entry.Stamp)?.Texture, new Vector2(14, 12)));
            row.AddChild(new Label { Text = $"{entry.Name} · {entry.At.ToLocalTime():dd/MM HH:mm}" });
            _rows.AddChild(row);
        }

        _rows.AddChild(WindowRows.Heading($"Carte des spécialités · {tavern.Tasted.Count} goûtées"));
        foreach (TastedData tasted in tavern.Tasted)
        {
            HBoxContainer row = new();
            row.AddChild(new ColorRect { Color = SpecialtyColors.Of(tasted.Color), CustomMinimumSize = new Vector2(12, 12), SizeFlagsVertical = SizeFlags.ShrinkCenter });
            row.AddChild(new Label { Text = $"{tasted.Name}, chez {tasted.Host}" });
            _rows.AddChild(row);
        }
    }

    private PanelContainer FriendRow(FriendData friend, bool canVisit)
    {
        HBoxContainer row = new();
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(Icon(PatronSprites.For(VisitDesk.Look(friend.Avatar), Gaze.Front).Texture, PortraitSize));
        VBoxContainer text = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        Label name = new() { Text = friend.Online ? $"{friend.Name} · en ligne" : friend.Name };
        name.AddThemeColorOverride("font_color", friend.Online ? BarPalette.Success : BarPalette.Text);
        text.AddChild(name);
        text.AddChild(WindowRows.Muted($"Spécialité : {friend.Specialty}"));
        row.AddChild(text);
        Button visit = Action("Rendre visite", () => VisitRequested?.Invoke(friend.Id, SelectedStamp()));
        visit.Disabled = !canVisit;
        row.AddChild(visit);
        Button remove = Action("×", () => Removed?.Invoke(friend.Id));
        remove.TooltipText = "Retirer de tes amis";
        row.AddChild(remove);
        return WindowRows.Card(row);
    }

    private string SelectedStamp() => _world?.Stamps.ElementAtOrDefault(Math.Max(_stamps.Selected, 0))?.Id ?? "heart";

    private void RequestFriend()
    {
        if (_friendCode.Text.Trim().Length > 0)
        {
            FriendRequested?.Invoke(_friendCode.Text.Trim());
            _friendCode.Text = string.Empty;
        }
    }

    private static Button Action(string text, Action pressed)
    {
        Button button = new() { Text = text, FocusMode = FocusModeEnum.None, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        button.Pressed += pressed;
        return button;
    }

    private static TextureRect Icon(Texture2D? texture, Vector2 size) => new()
    {
        Texture = texture,
        CustomMinimumSize = size,
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = TextureFilterEnum.Nearest,
        SizeFlagsVertical = SizeFlags.ShrinkCenter,
    };
}
