using System;
using System.Linq;
using Godot;
using IdleBar.Inn;
using IdleBar.Online;

namespace IdleBar.Ui;

public sealed class StreetDesk
{
    private const double BoostSeconds = 300;
    private const float BannerSeconds = 6;

    private readonly Tavern _tavern;
    private readonly GameActions _actions;
    private readonly RoundMemory _memory = new();

    public StreetDesk(Tavern tavern, GameActions actions, Doorbell doorbell)
    {
        _tavern = tavern;
        _actions = actions;
        doorbell.Waved += wave => Announced?.Invoke($"{wave.From} te salue depuis la rue !", BannerSeconds);
    }

    public event Action<string, float>? Announced;

    public event Action<string, int>? Cheered;

    public Street Street { get; } = new();

    public void Sync(TavernData? tavern)
    {
        Street.Sync(tavern?.Passersby ?? []);
        foreach (RoundData round in (tavern?.Rounds ?? []).Where(round => _memory.IsNew(round.At)).OrderBy(round => round.At))
        {
            double age = (DateTimeOffset.Now - round.At).TotalSeconds;
            if (age < BoostSeconds)
            {
                Announced?.Invoke($"{round.From} offre une tournée générale ! Pourboires doublés pendant 5 minutes (+30 écus)", BannerSeconds);
                _tavern.Boost((float)(BoostSeconds - Math.Max(0, age)));
                foreach (Patron patron in _tavern.Patrons.Where(patron => patron.Seated))
                {
                    Cheered?.Invoke("Santé !", (int)MathF.Round(patron.X));
                }
            }
            else
            {
                Announced?.Invoke($"Pendant ton absence, {round.From} t'a offert une tournée (+30 écus)", BannerSeconds);
            }

            _memory.Remember(round.At);
        }
    }

    public async void Greet(PasserbyData passerby)
    {
        string? error = await ActionFeedback.CaptureAsync(() => _actions.GreetAsync(passerby.Id));
        Announced?.Invoke(error ?? $"Tu salues {passerby.Name} par la fenêtre", BannerSeconds);
    }

    public async void Invite(PasserbyData passerby)
    {
        string? error = await ActionFeedback.CaptureAsync(() => _actions.InviteAsync(passerby.Id));
        Announced?.Invoke(error ?? $"Invitation envoyée à {passerby.Name} : s'il accepte, vous serez amis et il viendra boire un verre", BannerSeconds);
    }
}
