using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed partial class SocialSession
{
    public const string Unavailable = "unavailable";

    private const double PollSeconds = 60;

    private readonly FrontierSession _frontier;
    private readonly SocialApi _api;
    private double _sincePoll = PollSeconds;
    private int _generation;
    private HashSet<long>? _known;
    private bool _busy;
    private int _sent;
    private int _accepted;
    private FrontierGame? _seen;
    private int _worn = -1;

    public SocialSession(FrontierSession frontier, SocialApi api)
    {
        _frontier = frontier;
        _api = api;
        _frontier.Saved += () =>
        {
            if (_frontier.SavedRetreats != _worn)
            {
                RefreshSoon();
            }
        };
    }

    public event Action? Changed;

    public SocialData? Data { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    public string? Failure { get; private set; }

    public double Left(double seconds) => Math.Max(0, seconds - (DateTimeOffset.UtcNow - ReceivedAt).TotalSeconds);

    public void Tick(double delta)
    {
        if (_frontier.Status != SessionStatus.Ready || _frontier.Game is null)
        {
            return;
        }

        _sincePoll += delta;
        if (!_busy && _sincePoll >= PollSeconds)
        {
            _ = RunAsync(_api.GetAsync, false);
        }
    }

    public void RefreshSoon() => _sincePoll = PollSeconds;

    public void Reset()
    {
        _generation++;
        _busy = false;
        _sincePoll = PollSeconds;
        Data = null;
        _known = null;
        Changed?.Invoke();
    }

    public Task<string> RefreshAsync() => RunAsync(_api.GetAsync);

    public Task<string> CreateGuildAsync(string name) => RunAsync(token => _api.CreateGuildAsync(token, name));

    public Task<string> JoinGuildAsync(string code) => RunAsync(token => _api.JoinGuildAsync(token, code));

    public Task<string> LeaveGuildAsync() => RunAsync(_api.LeaveGuildAsync);

    public Task<string> KickAsync(long member) => RunAsync(token => _api.KickAsync(token, member));

    public Task<string> RenameGuildAsync(string name) => RunAsync(token => _api.RenameGuildAsync(token, name));

    public Task<string> NewCodeAsync() => RunAsync(_api.NewCodeAsync);

    public Task<string> SendHelpAsync(long member, string kind) => RunAsync(token => _api.SendHelpAsync(token, member, kind));

    public Task<string> ClaimTitanAsync() => RunAsync(_api.ClaimTitanAsync);

    private async Task<string> RunAsync(Func<string, Task<SocialData>> call, bool report = true)
    {
        int generation = _generation;
        int ticket = ++_sent;
        int retreats = _frontier.Game?.State.Retreats ?? 0;
        _busy = true;
        Failure = report ? null : Failure;
        try
        {
            SocialData data = await call(await _frontier.TokenAsync());
            if (generation != _generation)
            {
                return Unavailable;
            }

            if (ticket > _accepted)
            {
                _accepted = ticket;
                Accept(data, retreats);
            }

            return data.Status;
        }
        catch (Exception exception) when (exception is ActionRefusedException or CloudAuthException or OutdatedClientException
            || TransportFailure.Matches(exception))
        {
            GD.PushWarning($"Guilde injoignable : {exception.Message}");
            Failure = report ? ServerFailure.Reason(exception) : Failure;
            return Unavailable;
        }
        finally
        {
            if (generation == _generation)
            {
                _busy = false;
                _sincePoll = 0;
            }
        }
    }

    private void Accept(SocialData data, int retreats)
    {
        Data = data;
        ReceivedAt = DateTimeOffset.UtcNow;
        bool quiet = _seen is not null && _seen != _frontier.Game;
        _seen = _frontier.Game ?? _seen;
        if (_frontier.Game is FrontierGame game && data.Help is HelpData help)
        {
            game.Reinforce([.. help.Allies.Select(ally => ally.ToAlly())], [.. help.Boosts.Select(boost => boost.ToBoost())], !quiet);
        }

        if (_frontier.Game is FrontierGame watcher && data.Titan is TitanData titan)
        {
            watcher.Sight(titan.ToView(), !quiet);
        }

        _frontier.Game?.Study(data.ResearchLevels, !quiet);

        bool settled = _frontier.Game?.State.Retreats == retreats && _frontier.SavedRetreats == retreats;
        if (_frontier.Game is FrontierGame wearer && data.Items is ItemsData items)
        {
            if (settled)
            {
                foreach (FighterInfo fighter in Fighters.All)
                {
                    wearer.Equip(fighter.Kind, items.ToGear(fighter.Kind), items.ToWorn(fighter.Kind));
                }
            }

            Discover(wearer, items);
        }

        if (settled)
        {
            _worn = retreats;
        }

        Changed?.Invoke();
    }

    private void Discover(FrontierGame game, ItemsData items)
    {
        if (_known is not null)
        {
            foreach (ItemData item in items.Bag.Where(item => item.Source != Items.ShopSource && !_known.Contains(item.Id)))
            {
                game.Discover(item.Rank, item.Name);
            }
        }

        _known = [.. items.Bag.Select(item => item.Id)];
    }
}
