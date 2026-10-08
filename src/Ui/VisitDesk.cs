using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Inn;
using IdleBar.Online;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public sealed class VisitDesk
{
    private const string Cheers = "cheers";
    private const float BannerSeconds = 5;

    private readonly Tavern _tavern;
    private readonly GameActions _actions;
    private readonly Dictionary<long, PatronLook> _looks = [];
    private readonly Dictionary<long, string> _names = [];
    private readonly Dictionary<long, Guid> _players = [];
    private OutingData? _outing;

    public VisitDesk(Tavern tavern, GameActions actions, Doorbell doorbell)
    {
        _tavern = tavern;
        _actions = actions;
        tavern.GuestServed += OnGuestServed;
        doorbell.Emoted += OnEmoted;
    }

    public event Action<string, int, Color>? Popped;

    public event Action<string, float>? Announced;

    public static string Text(string emote) => emote switch
    {
        "cheers" => "Santé !",
        "thanks" => "Merci !",
        "laugh" => "Ha ha !",
        _ => "…",
    };

    public static PatronLook Look(AvatarData avatar) => PatronLook.Compose(avatar.Head, avatar.Skin, avatar.Hair, avatar.Clothes, avatar.Accent);

    public void Sync(TavernData? tavern)
    {
        IReadOnlyList<GuestData> waiting = tavern?.Guests ?? [];
        foreach (GuestData guest in waiting)
        {
            _looks[guest.Visit] = Look(guest.Avatar);
            _names[guest.Visit] = guest.Name;
        }

        IReadOnlyList<RoomGuest> present = tavern?.Room?.Guests ?? [];
        foreach (RoomGuest guest in present)
        {
            _looks[guest.Visit] = Look(guest.Avatar);
            _names[guest.Visit] = guest.Name;
            _players[guest.Visit] = guest.Id;
        }

        _tavern.Expect(tavern?.Room is { Mine: true }
            ? present.Select(Guest).ToList()
            : waiting.Select(guest => new GuestVisit(guest.Visit, guest.Name, DrinkMenu.FromId(guest.Drink) ?? Drink.Beer)).ToList());
        _outing = tavern?.Outing;
    }

    public static GuestVisit Guest(RoomGuest guest) =>
        new(guest.Visit, guest.Name, DrinkMenu.FromId(guest.Drink) ?? Drink.Beer, guest.Served, !guest.Online, guest.OrderedAt);

    public PatronLook? LookOf(Patron patron) =>
        patron.Visit is long visit && _looks.TryGetValue(visit, out PatronLook? look) ? look : RegularLooks.For(patron.Regular);

    public Guid? PlayerOf(Patron patron) => patron.Visit is long visit && _players.TryGetValue(visit, out Guid player) ? player : null;

    public string? Describe(Patron patron, bool hosting)
    {
        if (patron.Visit is not long visit || !_names.TryGetValue(visit, out string? name))
        {
            return null;
        }

        return hosting
            ? $"{name}, en visite · commande : {DrinkMenu.Name(patron.Order)} · clic : trinquer · clic droit : options"
            : $"{name}, en visite · clic droit : options";
    }

    public void Cheer(Patron patron)
    {
        if (patron.Visit is long visit)
        {
            Popped?.Invoke(Text(Cheers), (int)MathF.Round(patron.X), BarPalette.Gold);
            Send(visit, Cheers);
        }
    }

    public void Emote(string emote)
    {
        if (_outing is OutingData outing)
        {
            Send(outing.Visit, emote);
        }
    }

    private async void Send(long visit, string emote)
    {
        string? error = await ActionFeedback.CaptureAsync(() => _actions.SendEmoteAsync(visit, emote));
        if (error is not null)
        {
            Announced?.Invoke(error, BannerSeconds);
        }
    }

    private async void OnGuestServed(GuestService service)
    {
        Popped?.Invoke(service.Perfect ? "Ami servi, parfait !" : "Ami servi !", service.X, BarPalette.Gold);
        string? error = await ActionFeedback.CaptureAsync(() => _actions.ServeVisitAsync(service.Visit, service.Perfect));
        if (error is not null)
        {
            Announced?.Invoke(error, BannerSeconds);
        }
    }

    private void OnEmoted(EmoteData emote)
    {
        if (_tavern.Patrons.FirstOrDefault(patron => patron.Visit == emote.Visit) is Patron guest)
        {
            Popped?.Invoke(Text(emote.Emote), (int)MathF.Round(guest.X), BarPalette.Text);
            return;
        }

        Announced?.Invoke($"{emote.From} : {Text(emote.Emote)}", BannerSeconds);
    }
}
