using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using IdleBar.Cloud;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed class SocialSession
{
    public const string Unavailable = "unavailable";

    private const double PollSeconds = 60;

    private readonly FrontierSession _frontier;
    private readonly SocialApi _api;
    private double _sincePoll = PollSeconds;
    private int _generation;
    private bool _busy;

    public SocialSession(FrontierSession frontier, SocialApi api)
    {
        _frontier = frontier;
        _api = api;
    }

    public event Action? Changed;

    public SocialData? Data { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

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
            _ = RefreshAsync();
        }
    }

    public void RefreshSoon() => _sincePoll = PollSeconds;

    public void Reset()
    {
        _generation++;
        _busy = false;
        _sincePoll = PollSeconds;
        Data = null;
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

    private async Task<string> RunAsync(Func<string, Task<SocialData>> call)
    {
        int generation = _generation;
        _busy = true;
        try
        {
            SocialData data = await call(await _frontier.TokenAsync());
            if (generation != _generation)
            {
                return Unavailable;
            }

            Accept(data);
            return data.Status;
        }
        catch (Exception exception) when (exception is ActionRefusedException or CloudAuthException or OutdatedClientException
            || TransportFailure.Matches(exception))
        {
            GD.PushWarning($"Guilde injoignable : {exception.Message}");
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

    private void Accept(SocialData data)
    {
        Data = data;
        ReceivedAt = DateTimeOffset.UtcNow;
        if (_frontier.Game is FrontierGame game && data.Help is HelpData help)
        {
            game.Reinforce([.. help.Allies.Select(ally => ally.ToAlly())], [.. help.Boosts.Select(boost => boost.ToBoost())]);
        }

        Changed?.Invoke();
    }
}
