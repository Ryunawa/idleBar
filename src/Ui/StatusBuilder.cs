using System;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public static class StatusBuilder
{
    private const string Title = "La Frontière";

    public static BarStatus Describe(FrontierSession? session, FrontierGame? game)
    {
        if (session is null)
        {
            return Waiting("IdleBar", "Supabase n'est pas configuré");
        }

        return (session.Status, game) switch
        {
            (SessionStatus.SignedOut, _) => Waiting("Bienvenue !", "Clique ici pour te connecter"),
            (SessionStatus.UpdateRequired, _) => Waiting("Mise à jour requise", "Clique ici pour la télécharger"),
            (SessionStatus.NeedsStart, _) => Waiting(Title, "Clique ici pour prendre la route"),
            (SessionStatus.Elsewhere, FrontierGame paused) => Playing(paused.State, "Partie ouverte ailleurs · clique pour la reprendre ici", true),
            (SessionStatus.Offline, null) => Waiting("Hors ligne", "Serveur injoignable"),
            (_, null) => Waiting("Connexion…", string.Empty),
            (SessionStatus.Offline, FrontierGame offline) => Playing(offline.State, Situation(offline.State) + " · hors ligne", false),
            (_, FrontierGame playing) => Playing(playing.State, Situation(playing.State), playing.State.Monster is { Boss: true }),
        };
    }

    public static string Situation(FrontierState state)
    {
        if (state.Monster is { Boss: true } boss)
        {
            return $"BOSS · {Math.Ceiling(Math.Max(0, boss.BossLeft))} s";
        }

        string rate = $"+{NumberFormat.Rate(Economy.GoldPerSecond(state))}/s";
        string wave = state.BossHeld ? "boss en attente" : $"{state.Kills}/{Balance.MonstersPerStage}";
        int pending = RetreatRules.Pending(state);
        string souls = pending > 0 ? $" · repli : +{NumberFormat.Count(pending, "âme", "âmes")}" : string.Empty;
        string boost = state.Boosts.Count > 0 ? $" (or ×{NumberFormat.Factor(Help.GoldFactor(state))})" : string.Empty;
        return $"{rate}{boost} · niv {state.Stage} · {wave}{souls}";
    }

    private static BarStatus Playing(FrontierState state, string situation, bool alert) =>
        new(Title, state.Gold, situation, $"{NumberFormat.Gold(state.Gold)} · niv {state.Stage}", alert);

    private static BarStatus Waiting(string headline, string situation) =>
        new(headline, null, situation, $"{headline} {situation}".Trim());
}
