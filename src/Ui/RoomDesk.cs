using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdleBar.Inn;
using IdleBar.Online;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public sealed class RoomDesk
{
    private const float RefusalSeconds = 5;

    private readonly GameActions _actions;
    private Guid? _host;
    private Guid? _awayHost;
    private long _lastLine;
    private bool _heard;

    public RoomDesk(GameActions actions, Doorbell doorbell)
    {
        _actions = actions;
        doorbell.Chatted += Hear;
    }

    public event Action<string, float>? Announced;

    public event Action<ChatLine>? Spoken;

    public event Action? ViewChanged;

    public RoomData? Room { get; private set; }

    public Tavern? Away { get; private set; }

    public DecorSet Decor { get; private set; } = DecorSet.Bare;

    public IReadOnlyList<string> Souvenirs { get; private set; } = [];

    public bool CanSpeak => Room is { Mine: false } or { Guests.Count: > 0 };

    public PatronLook? HostLook => Room is { Mine: false, HostHome: true } room ? VisitDesk.Look(room.HostAvatar) : null;

    public void Sync(TavernData? tavern, WorldData? world)
    {
        RoomData? room = tavern?.Room;
        Room = room;
        if (room?.HostId != _host)
        {
            _host = room?.HostId;
            _heard = false;
        }

        foreach (ChatLine line in room?.Chat ?? [])
        {
            if (_heard)
            {
                Hear(line);
            }
            else
            {
                _lastLine = Math.Max(_lastLine, line.Id);
            }
        }

        _heard = room is not null;
        if (room is not { Mine: false })
        {
            if (Away is not null)
            {
                Away = null;
                ViewChanged?.Invoke();
            }

            return;
        }

        if (Away is null || _awayHost != room.HostId)
        {
            Away = new Tavern(new Random()) { Interactive = false, AutoServe = true };
            _awayHost = room.HostId;
            ViewChanged?.Invoke();
        }

        TavernSetup.Configure(Away, room.Stools, room.Menu, room.Helper);
        Away.Expect(room.Guests.Select(VisitDesk.Guest).ToList());
        Decor = TavernSetup.Decor(world, room.Upgrades);
        Souvenirs = room.Souvenirs;
    }

    public void Tick(float delta) => Away?.Update(delta);

    public void Say(string text) => Run(() => _actions.SayAsync(text));

    public void Order(string drink) => Run(() => _actions.OrderDrinkAsync(drink));

    public void Leave() => Run(_actions.LeaveVisitAsync);

    public void ShowDoor(long visit) => Run(() => _actions.ShowDoorAsync(visit));

    public void Mute(Guid player, bool muted) => Run(() => _actions.MuteAsync(player, muted));

    private async void Run(Func<Task> action)
    {
        string? error = await ActionFeedback.CaptureAsync(action);
        if (error is not null)
        {
            Announced?.Invoke(error, RefusalSeconds);
        }
    }

    private void Hear(ChatLine line)
    {
        if (line.Id <= _lastLine || (line.Room is Guid room && room != _host))
        {
            return;
        }

        _lastLine = line.Id;
        Spoken?.Invoke(line);
    }
}
