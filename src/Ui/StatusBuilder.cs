using System;
using IdleBar.Frontier;
using IdleBar.Online;

namespace IdleBar.Ui;

public static class StatusBuilder
{
    private const string Title = "La Frontière";

    public static BarStatus Describe(FrontierSession? session, FrontierGame game)
    {
        if (session is null)
        {
            return Waiting("IdleBar", "Supabase n'est pas configuré");
        }

        return session.Status switch
        {
            SessionStatus.SignedOut => Waiting("Bienvenue !", "Clique ici pour te connecter"),
            SessionStatus.NeedsStart => Waiting(Title, "Clique ici pour prendre la route"),
            SessionStatus.UpdateRequired => Waiting("Mise à jour requise", "Clique ici pour la télécharger"),
            SessionStatus.Connecting => Waiting("Connexion…", string.Empty),
            SessionStatus.Offline => Playing(game.State, " · hors ligne"),
            _ => Playing(game.State, string.Empty),
        };
    }

    public static string Situation(FrontierState state)
    {
        if (state.Monster is { Boss: true } boss)
        {
            return $"BOSS · {Math.Ceiling(Math.Max(0, boss.BossLeft))} s";
        }

        string rate = $"+{NumberFormat.Rate(Economy.GoldPerSecond(state))}/s";
        return $"{rate} · niv {state.Stage} · {state.Kills}/{Balance.MonstersPerStage}";
    }

    private static BarStatus Playing(FrontierState state, string suffix)
    {
        string situation = Situation(state) + suffix;
        return new BarStatus(Title, state.Gold, situation, $"{NumberFormat.Gold(state.Gold)} · niv {state.Stage}{suffix}", state.Monster is { Boss: true });
    }

    private static BarStatus Waiting(string headline, string situation) =>
        new(headline, null, situation, $"{headline} {situation}".Trim());
}
